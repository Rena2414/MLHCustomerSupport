from typing import Any, Dict, List, Tuple

from app.core.messages import Message, ToolCall
from app.core.model_client import ModelClient, ModelTurn
from app.core.tools import ToolSpec
from app.tools.backend_api import BackendApi


class ScriptedModel(ModelClient):
    def __init__(self, turns: List[ModelTurn]) -> None:
        self._turns = list(turns)
        self.calls: List[Tuple[str, List[Message], List[str]]] = []

    def generate(self, system_prompt: str, messages: List[Message], tools: List[ToolSpec]) -> ModelTurn:
        self.calls.append((system_prompt, list(messages), [t.name for t in tools]))
        return self._turns.pop(0)


def tool_turn(name: str, **arguments) -> ModelTurn:
    return ModelTurn(tool_calls=[ToolCall(id=f"call-{name}", name=name, arguments=arguments)])


def final_turn(text: str) -> ModelTurn:
    return ModelTurn(text=text)


class FakeBackendApi(BackendApi):
    def __init__(self, context=None, fail_with=None) -> None:
        self.context = context or {
            "name": "Mia",
            "age": 11,
            "subscriptionType": 1,
            "teachers": [{"id": "teacher-1", "name": "Mr Brown", "class": "Maths"}],
        }
        self.fail_with = fail_with
        self.cancelled: List[str] = []
        self.concerns: List[Dict[str, Any]] = []
        self.context_requests: List[str] = []

    def get_student_context(self, student_id: str) -> Dict[str, Any]:
        self.context_requests.append(student_id)
        return self.context

    def cancel_subscription(self, student_id: str) -> Dict[str, Any]:
        if self.fail_with:
            raise self.fail_with
        self.cancelled.append(student_id)
        return {"subscriptionType": 0}

    def create_concern(self, **fields: Any) -> Dict[str, Any]:
        if self.fail_with:
            raise self.fail_with
        self.concerns.append(fields)
        return {"id": f"concern-{len(self.concerns)}"}
