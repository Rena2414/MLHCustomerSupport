from dataclasses import dataclass, field
from typing import Any, Dict, List


@dataclass
class ToolCall:
    id: str
    name: str
    arguments: Dict[str, Any]


@dataclass
class ToolResult:
    call_id: str
    name: str
    content: Dict[str, Any]
    is_error: bool = False


@dataclass
class Message:
    role: str
    text: str = ""
    tool_calls: List[ToolCall] = field(default_factory=list)
    tool_results: List[ToolResult] = field(default_factory=list)
