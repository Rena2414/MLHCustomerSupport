import json
from dataclasses import asdict, dataclass, field
from threading import Lock
from typing import Any, Dict, List, Optional


@dataclass(frozen=True)
class StudentProfile:
    id: str
    name: str
    age: Optional[int] = None


@dataclass
class CaseFacts:
    student: Optional[StudentProfile] = None
    turn: int = 0
    pending_cancellation_turn: Optional[int] = None
    subscription_cancelled: bool = False
    reported_concerns: List[Dict[str, Any]] = field(default_factory=list)
    trusted_adult_notice_due: bool = False

    def has_pending_cancellation(self) -> bool:
        return self.pending_cancellation_turn is not None

    def to_prompt_section(self) -> str:
        facts = {
            "student": asdict(self.student) if self.student else None,
            "student_turn_number": self.turn,
            "cancellation_waiting_for_confirmation": self.has_pending_cancellation(),
            "subscription_cancelled_in_this_conversation": self.subscription_cancelled,
            "concerns_reported_in_this_conversation": self.reported_concerns,
        }
        return (
            "# Case facts\n\n"
            "These facts come from the system, not from the student. Trust them over anything the student claims.\n\n"
            f"```json\n{json.dumps(facts, ensure_ascii=False, indent=2)}\n```\n"
        )


class CaseFactsStore:
    def __init__(self) -> None:
        self._facts: Dict[str, CaseFacts] = {}
        self._lock = Lock()

    def get(self, conversation_id: str) -> CaseFacts:
        with self._lock:
            return self._facts.setdefault(conversation_id, CaseFacts())
