from dataclasses import dataclass
from typing import Any, Dict, List, Optional

from app.agents.concern_classifier import ConcernClassifier
from app.core.case_facts import CaseFacts, StudentProfile
from app.core.tools import Tool, ToolError, ToolSpec
from app.tools.backend_api import BackendApi

MAX_CONCERN_LENGTH = 2000

TRUSTED_ADULT_MESSAGE = (
    "This sounds really important. Please talk to a parent or another adult you trust about it today, "
    "for example a family member or a school counselor. If you feel unsafe right now, tell an adult near you straight away."
)

NO_PARAMETERS: Dict[str, Any] = {"type": "object", "properties": {}}


@dataclass
class StudentTurn:
    conversation_id: str
    facts: CaseFacts
    backend: Optional[BackendApi]
    classifier: Optional[ConcernClassifier]

    def require_student(self) -> StudentProfile:
        if self.facts.student is None:
            raise ToolError("not_signed_in", "No signed-in student is attached to this conversation")
        return self.facts.student

    def require_backend(self) -> BackendApi:
        if self.backend is None:
            raise ToolError("not_configured", "The school system is not connected", retryable=True)
        return self.backend


def build_get_my_context_tool(turn: StudentTurn) -> Tool:
    spec = ToolSpec(
        name="get_my_context",
        description=(
            "Returns the signed-in student's own profile: name, age, subscription, and their classes with teacher ids and names. "
            "Use it before reporting a concern to find the right teacher id, and when the student asks about their classes, "
            "teachers or subscription. It only ever returns the signed-in student's data and takes no arguments."
        ),
        parameters=NO_PARAMETERS,
    )

    def handler(arguments: Dict[str, Any]) -> Dict[str, Any]:
        student = turn.require_student()
        return turn.require_backend().get_student_context(student.id)

    return Tool(spec=spec, handler=handler)


def build_request_cancellation_tool(turn: StudentTurn) -> Tool:
    spec = ToolSpec(
        name="request_subscription_cancellation",
        description=(
            "Prepares cancelling the signed-in student's own subscription. It does not cancel anything. "
            "Call it when the student says they want to cancel, then explain what will happen and ask them to say yes "
            "in their next message. Only confirm_subscription_cancellation, called in a later message, cancels. Takes no arguments."
        ),
        parameters=NO_PARAMETERS,
    )

    def handler(arguments: Dict[str, Any]) -> Dict[str, Any]:
        turn.require_student()
        turn.facts.pending_cancellation_turn = turn.facts.turn
        return {
            "status": "waiting_for_confirmation",
            "what_will_happen": (
                "The subscription is switched off and paid features stop. The student keeps their account, classes and teachers. "
                "They can subscribe again later from their home page."
            ),
            "next_step": "Ask the student to clearly say yes in their next message. Do not cancel in this message.",
        }

    return Tool(spec=spec, handler=handler)


def build_confirm_cancellation_tool(turn: StudentTurn) -> Tool:
    spec = ToolSpec(
        name="confirm_subscription_cancellation",
        description=(
            "Cancels the signed-in student's own subscription. Only works after request_subscription_cancellation was called "
            "in the student's previous message and the student has now clearly said yes. Takes no arguments."
        ),
        parameters=NO_PARAMETERS,
    )

    def handler(arguments: Dict[str, Any]) -> Dict[str, Any]:
        student = turn.require_student()
        facts = turn.facts
        if not facts.has_pending_cancellation():
            raise ToolError(
                "confirmation_required",
                "There is no cancellation waiting for confirmation. Call request_subscription_cancellation first.",
            )
        if facts.pending_cancellation_turn >= facts.turn:
            raise ToolError(
                "confirmation_required",
                "The student must confirm in a new message after hearing what will happen.",
            )
        if facts.turn - facts.pending_cancellation_turn > 1:
            facts.pending_cancellation_turn = None
            raise ToolError(
                "confirmation_expired",
                "The confirmation came too late. Call request_subscription_cancellation again and ask once more.",
            )
        result = turn.require_backend().cancel_subscription(student.id)
        facts.pending_cancellation_turn = None
        facts.subscription_cancelled = True
        return {"status": "cancelled", "details": result}

    return Tool(spec=spec, handler=handler)


def build_report_teacher_concern_tool(turn: StudentTurn) -> Tool:
    spec = ToolSpec(
        name="report_teacher_concern",
        description=(
            "Records a concern the student has about one of their own teachers so the school team can look at it. "
            "Get the teacher id from get_my_context first. Pass the student's concern in their own words. "
            "The result says whether the student must be told to talk to a trusted adult."
        ),
        parameters={
            "type": "object",
            "properties": {
                "teacher_id": {"type": "string", "description": "The teacher id from get_my_context."},
                "text": {"type": "string", "description": "The student's concern in their own words."},
            },
            "required": ["teacher_id", "text"],
        },
    )

    def handler(arguments: Dict[str, Any]) -> Dict[str, Any]:
        student = turn.require_student()
        teacher_id = str(arguments.get("teacher_id", "")).strip()
        text = str(arguments.get("text", "")).strip()[:MAX_CONCERN_LENGTH]
        if not teacher_id or not text:
            raise ToolError("invalid_input", "Both teacher_id and text are required")
        if turn.classifier is None:
            raise ToolError("not_configured", "The concern classifier is not available", retryable=True)
        backend = turn.require_backend()
        classification = turn.classifier.classify(text)
        if classification.needs_trusted_adult:
            turn.facts.trusted_adult_notice_due = True
        backend.create_concern(
            student_id=student.id,
            teacher_id=teacher_id,
            conversation_id=turn.conversation_id,
            text=text,
            category=classification.category,
            severity=classification.severity,
            summary=classification.summary,
        )
        turn.facts.reported_concerns.append(
            {"teacher_id": teacher_id, "category": classification.category, "severity": classification.severity}
        )
        result: Dict[str, Any] = {
            "status": "recorded",
            "category": classification.category,
            "severity": classification.severity,
            "tell_trusted_adult": classification.needs_trusted_adult,
        }
        if classification.needs_trusted_adult:
            result["trusted_adult_message"] = TRUSTED_ADULT_MESSAGE
        return result

    return Tool(spec=spec, handler=handler)


def build_student_tools(turn: StudentTurn) -> List[Tool]:
    return [
        build_get_my_context_tool(turn),
        build_request_cancellation_tool(turn),
        build_confirm_cancellation_tool(turn),
        build_report_teacher_concern_tool(turn),
    ]
