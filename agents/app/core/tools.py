from dataclasses import dataclass
from typing import Any, Callable, Dict, List

from app.core.messages import ToolCall, ToolResult


class ToolError(Exception):
    def __init__(self, category: str, message: str, retryable: bool = False) -> None:
        super().__init__(message)
        self.category = category
        self.message = message
        self.retryable = retryable

    def to_payload(self) -> Dict[str, Any]:
        return {"error": {"category": self.category, "retryable": self.retryable, "message": self.message}}


@dataclass(frozen=True)
class ToolSpec:
    name: str
    description: str
    parameters: Dict[str, Any]


@dataclass(frozen=True)
class Tool:
    spec: ToolSpec
    handler: Callable[[Dict[str, Any]], Dict[str, Any]]


class ToolRegistry:
    def __init__(self, tools: List[Tool]) -> None:
        self._tools = {tool.spec.name: tool for tool in tools}

    def specs(self) -> List[ToolSpec]:
        return [tool.spec for tool in self._tools.values()]

    def execute(self, call: ToolCall) -> ToolResult:
        tool = self._tools.get(call.name)
        if tool is None:
            error = ToolError("unknown_tool", f"Tool '{call.name}' does not exist")
            return ToolResult(call.id, call.name, error.to_payload(), is_error=True)
        try:
            return ToolResult(call.id, call.name, tool.handler(call.arguments))
        except ToolError as error:
            return ToolResult(call.id, call.name, error.to_payload(), is_error=True)
        except Exception:
            error = ToolError("internal", "The tool failed unexpectedly")
            return ToolResult(call.id, call.name, error.to_payload(), is_error=True)
