from pathlib import Path
from typing import Any, Dict

from app.core.tools import Tool, ToolError, ToolSpec


def build_read_help_guide_tool(doc_path: Path) -> Tool:
    spec = ToolSpec(
        name="read_help_guide",
        description=(
            "Returns the full text of the Classly help guide, a set of questions and answers about the learning platform. "
            "Call it once, then answer from it. It takes no arguments."
        ),
        parameters={"type": "object", "properties": {}},
    )

    def handler(arguments: Dict[str, Any]) -> Dict[str, Any]:
        try:
            content = doc_path.read_text(encoding="utf-8")
        except FileNotFoundError:
            raise ToolError("not_found", "The help guide does not exist")
        except OSError:
            raise ToolError("io_error", "The help guide could not be read", retryable=True)
        return {"document": "Classly help guide", "content": content}

    return Tool(spec=spec, handler=handler)
