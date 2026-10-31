import json
import re
from typing import Any, Dict

FENCE_PATTERN = re.compile(r"^```(?:json)?\s*|\s*```$", re.IGNORECASE)


class JsonOutputError(Exception):
    pass


def parse_json_object(text: str) -> Dict[str, Any]:
    cleaned = FENCE_PATTERN.sub("", text.strip()).strip()
    start = cleaned.find("{")
    end = cleaned.rfind("}")
    if start == -1 or end < start:
        raise JsonOutputError("The model did not return a JSON object")
    try:
        value = json.loads(cleaned[start : end + 1])
    except json.JSONDecodeError as error:
        raise JsonOutputError("The model returned invalid JSON") from error
    if not isinstance(value, dict):
        raise JsonOutputError("The model did not return a JSON object")
    return value
