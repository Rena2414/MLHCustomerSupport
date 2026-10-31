from typing import Any, Dict, Iterator, Union

from app.agents.docs_reader import DocsReaderAgent
from app.core.agent_loop import AgentLoopError, AgentResult, stream_agent
from app.core.conversation_store import ConversationStore
from app.core.messages import Message
from app.core.model_client import ModelClient, TextDelta
from app.core.prompts import load_prompt
from app.core.tools import Tool, ToolError, ToolRegistry, ToolSpec


class OrchestratorAgent:
    def __init__(
        self,
        model: ModelClient,
        store: ConversationStore,
        docs_reader: DocsReaderAgent,
        max_iterations: int = 25,
    ) -> None:
        self._model = model
        self._store = store
        self._max_iterations = max_iterations
        self._registry = ToolRegistry([self._build_ask_help_guide_tool(docs_reader)])

    def handle(self, conversation_id: str, user_text: str) -> str:
        for item in self.handle_stream(conversation_id, user_text):
            if isinstance(item, AgentResult):
                return item.text
        raise AgentLoopError("The agent finished without a result")

    def handle_stream(self, conversation_id: str, user_text: str) -> Iterator[Union[TextDelta, AgentResult]]:
        user_message = Message(role="user", text=user_text)
        history = self._store.get(conversation_id)
        history.append(user_message)
        self._store.append(conversation_id, [user_message])
        for item in stream_agent(
            model=self._model,
            system_prompt=load_prompt("orchestrator"),
            history=history,
            registry=self._registry,
            max_iterations=self._max_iterations,
        ):
            if isinstance(item, AgentResult):
                self._store.append(conversation_id, item.new_messages)
            yield item

    @staticmethod
    def _build_ask_help_guide_tool(docs_reader: DocsReaderAgent) -> Tool:
        spec = ToolSpec(
            name="ask_help_guide",
            description=(
                "Asks the help guide specialist a question about Leaply. "
                "The specialist reads the Leaply help guide and returns a short summary with concrete steps and examples. "
                "Use it for every question about the app, subscriptions, payments, refunds, accounts and getting help. "
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
