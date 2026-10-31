from dataclasses import dataclass

from app.core.json_output import JsonOutputError, parse_json_object
from app.core.messages import Message
from app.core.model_client import ModelClient, ModelError
from app.core.prompts import load_prompt

CATEGORIES = ("Communication", "Fairness", "TeachingQuality", "LatenessOrAbsence", "Behavior", "Safety", "Other")
SEVERITIES = ("Low", "Medium", "High", "Critical")
MAX_SUMMARY_LENGTH = 300


@dataclass(frozen=True)
class ConcernClassification:
    category: str
    severity: str
    summary: str

    @property
    def needs_trusted_adult(self) -> bool:
        return self.category == "Safety" or self.severity == "Critical"


def normalize_choice(value: object, choices: tuple) -> str:
    key = "".join(ch for ch in str(value or "") if ch.isalnum()).lower()
    for choice in choices:
        if choice.lower() == key:
            return choice
    return ""


class ConcernClassifier:
    def __init__(self, model: ModelClient) -> None:
        self._model = model

    def classify(self, text: str) -> ConcernClassification:
        try:
            turn = self._model.generate(load_prompt("concern_classifier"), [Message(role="user", text=text)], [])
            data = parse_json_object(turn.text)
        except (ModelError, JsonOutputError):
            return self._fallback(text)
        category = normalize_choice(data.get("category"), CATEGORIES)
        severity = normalize_choice(data.get("severity"), SEVERITIES)
        summary = str(data.get("summary") or "").strip()
        if not category or not severity:
            return self._fallback(text, summary)
        return ConcernClassification(category, severity, (summary or text)[:MAX_SUMMARY_LENGTH])

    @staticmethod
    def _fallback(text: str, summary: str = "") -> ConcernClassification:
        return ConcernClassification("Other", "High", (summary or text).strip()[:MAX_SUMMARY_LENGTH])
