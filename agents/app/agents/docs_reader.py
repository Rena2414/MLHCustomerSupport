from pathlib import Path

from app.core.agent_loop import run_agent
from app.core.messages import Message
from app.core.model_client import ModelClient
from app.core.prompts import load_prompt
from app.core.tools import ToolRegistry
from app.tools.help_guide import build_read_help_guide_tool


class DocsReaderAgent:
    def __init__(self, model: ModelClient, doc_path: Path, max_iterations: int = 25) -> None:
        self._model = model
        self._registry = ToolRegistry([build_read_help_guide_tool(doc_path)])
        self._max_iterations = max_iterations

    def answer(self, question: str) -> str:
        result = run_agent(
            model=self._model,
            system_prompt=load_prompt("docs_reader"),
            history=[Message(role="user", text=question)],
            registry=self._registry,
            max_iterations=self._max_iterations,
        )
        return result.text
