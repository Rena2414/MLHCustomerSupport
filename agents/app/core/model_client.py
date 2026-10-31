from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from typing import Iterator, List, Union

from app.core.messages import Message, ToolCall
from app.core.tools import ToolSpec


class ModelError(Exception):
    pass


@dataclass
class ModelTurn:
    text: str = ""
    tool_calls: List[ToolCall] = field(default_factory=list)

    @property
    def stop_reason(self) -> str:
        return "tool_use" if self.tool_calls else "end_turn"


@dataclass
class TextDelta:
    text: str


StreamItem = Union[TextDelta, ModelTurn]


class ModelClient(ABC):
    @abstractmethod
    def generate(self, system_prompt: str, messages: List[Message], tools: List[ToolSpec]) -> ModelTurn:
        ...

    def stream(self, system_prompt: str, messages: List[Message], tools: List[ToolSpec]) -> Iterator[StreamItem]:
        turn = self.generate(system_prompt, messages, tools)
        if turn.text:
            yield TextDelta(turn.text)
        yield turn
