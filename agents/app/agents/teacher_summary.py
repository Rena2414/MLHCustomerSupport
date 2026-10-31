import json
from dataclasses import dataclass, field
from typing import List

from app.core.json_output import JsonOutputError, parse_json_object
from app.core.messages import Message
from app.core.model_client import ModelClient
from app.core.prompts import load_prompt

MAX_ACTIONS = 5


@dataclass(frozen=True)
class ConcernRecord:
    text: str
    category: str
    severity: str
    created_at: str


@dataclass(frozen=True)
class TeacherSummary:
    summary: str
    recommended_actions: List[str] = field(default_factory=list)


class TeacherSummarizer:
    def __init__(self, model: ModelClient) -> None:
        self._model = model

    def summarize(self, teacher_name: str, concerns: List[ConcernRecord]) -> TeacherSummary:
        if not concerns:
            return TeacherSummary(summary=f"There are no concerns about {teacher_name}.", recommended_actions=[])
        payload = {
            "teacher_name": teacher_name,
            "concerns": [concern.__dict__ for concern in concerns],
        }
        turn = self._model.generate(
            load_prompt("teacher_summary"),
            [Message(role="user", text=json.dumps(payload, ensure_ascii=False))],
            [],
        )
        data = parse_json_object(turn.text)
        summary = str(data.get("summary") or "").strip()
        if not summary:
            raise JsonOutputError("The model returned an empty summary")
        actions = data.get("recommended_actions") or []
        if not isinstance(actions, list):
            actions = [actions]
        cleaned = [str(action).strip() for action in actions if str(action).strip()][:MAX_ACTIONS]
        return TeacherSummary(summary=summary, recommended_actions=cleaned)
