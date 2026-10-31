from typing import Any, Dict, Iterator, Optional, Union

from app.agents.concern_classifier import ConcernClassifier
from app.agents.docs_reader import DocsReaderAgent
from app.core.agent_loop import AgentLoopError, AgentResult, stream_agent
from app.core.case_facts import CaseFacts, CaseFactsStore, StudentProfile
from app.core.conversation_store import ConversationStore
from app.core.messages import Message
from app.core.model_client import ModelClient, TextDelta
from app.core.prompts import load_prompt
from app.core.tools import Tool, ToolError, ToolRegistry, ToolSpec
from app.tools.backend_api import BackendApi
from app.tools.student_tools import TRUSTED_ADULT_MESSAGE, StudentTurn, build_student_tools

NOTICE_SEPARATOR = "\n\n"


class OrchestratorAgent:
    def __init__(
        self,
        model: ModelClient,
        store: ConversationStore,
        docs_reader: DocsReaderAgent,
        max_iterations: int = 25,
        *,
        backend: Optional[BackendApi] = None,
        classifier: Optional[ConcernClassifier] = None,
        case_facts: Optional[CaseFactsStore] = None,
    ) -> None:
        self._model = model
        self._store = store
        self._max_iterations = max_iterations
        self._backend = backend
        self._classifier = classifier
        self._case_facts = case_facts or CaseFactsStore()
        self._ask_help_guide = self._build_ask_help_guide_tool(docs_reader)

    def facts_for(self, conversation_id: str) -> CaseFacts:
        return self._case_facts.get(conversation_id)

    def handle(self, conversation_id: str, user_text: str, student: Optional[StudentProfile] = None) -> str:
        for item in self.handle_stream(conversation_id, user_text, student):
            if isinstance(item, AgentResult):
                return item.text
        raise AgentLoopError("The agent finished without a result")

    def handle_stream(
        self, conversation_id: str, user_text: str, student: Optional[StudentProfile] = None
    ) -> Iterator[Union[TextDelta, AgentResult]]:
        facts = self._case_facts.get(conversation_id)
        if student is not None:
            facts.student = student
        facts.turn += 1
        user_message = Message(role="user", text=user_text)
        history = self._store.get(conversation_id)
        history.append(user_message)
        self._store.append(conversation_id, [user_message])
        turn = StudentTurn(conversation_id, facts, self._backend, self._classifier)
        registry = ToolRegistry([self._ask_help_guide, *build_student_tools(turn)])
        system_prompt = load_prompt("orchestrator") + "\n\n" + facts.to_prompt_section()
        for item in stream_agent(
            model=self._model,
            system_prompt=system_prompt,
            history=history,
            registry=registry,
            max_iterations=self._max_iterations,
        ):
            if isinstance(item, AgentResult):
                notice = self._take_trusted_adult_notice(facts, item.text)
                if notice:
                    yield TextDelta(notice)
                    item = self._with_notice(item, notice)
                self._store.append(conversation_id, item.new_messages)
            yield item

    @staticmethod
    def _take_trusted_adult_notice(facts: CaseFacts, reply: str) -> str:
        if not facts.trusted_adult_notice_due:
            return ""
        facts.trusted_adult_notice_due = False
        if TRUSTED_ADULT_MESSAGE in reply:
            return ""
        return (NOTICE_SEPARATOR if reply else "") + TRUSTED_ADULT_MESSAGE

    @staticmethod
    def _with_notice(result: AgentResult, notice: str) -> AgentResult:
        messages = list(result.new_messages)
        last = messages[-1]
        messages[-1] = Message(role=last.role, text=last.text + notice, tool_calls=last.tool_calls)
        return AgentResult(text=result.text + notice, new_messages=messages)

    @staticmethod
    def _build_ask_help_guide_tool(docs_reader: DocsReaderAgent) -> Tool:
        spec = ToolSpec(
            name="ask_help_guide",
            description=(
                "Asks the help guide specialist a question about Classly. "
                "The specialist reads the Classly help guide and returns a short summary with concrete steps. "
                "Use it for every question about how Classly works: signing in, classes, homework, profile, parents, "
                "subscription and reporting a concern. "
                "The question must make sense on its own because the specialist cannot see the conversation."
            ),
            parameters={
                "type": "object",
                "properties": {"question": {"type": "string", "description": "A self-contained question."}},
                "required": ["question"],
            },
        )

        def handler(arguments: Dict[str, Any]) -> Dict[str, Any]:
            question = str(arguments.get("question", "")).strip()
            if not question:
                raise ToolError("invalid_input", "The question must not be empty")
            return {"summary": docs_reader.answer(question)}

        return Tool(spec=spec, handler=handler)
