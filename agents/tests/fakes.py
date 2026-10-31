from typing import List, Tuple

from app.core.messages import Message, ToolCall
from app.core.model_client import ModelClient, ModelTurn
from app.core.tools import ToolSpec


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
