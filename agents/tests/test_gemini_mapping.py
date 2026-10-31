from types import SimpleNamespace

import pytest

from app.core.gemini_client import parse_response, to_contents, to_declarations
from app.core.messages import Message, ToolCall, ToolResult
from app.core.model_client import ModelError
from app.core.tools import ToolSpec


def test_history_maps_to_gemini_roles():
    messages = [
        Message("user", "q"),
        Message("assistant", "", [ToolCall("1", "ask", {"question": "x"})]),
        Message("tool", tool_results=[ToolResult("1", "ask", {"summary": "s"})]),
        Message("assistant", "answer"),
    ]
    roles = [c.role for c in to_contents(messages)]
    assert roles == ["user", "model", "user", "model"]


def test_declarations_use_json_schema():
    spec = ToolSpec("ask", "d", {"type": "object", "properties": {"question": {"type": "string"}}})
    declaration = to_declarations([spec])[0].function_declarations[0]
    assert declaration.name == "ask"
    assert declaration.parameters_json_schema["properties"]["question"]["type"] == "string"


def test_parse_function_call_and_text():
    call = SimpleNamespace(name="ask", args={"question": "x"})
    parts = [
        SimpleNamespace(function_call=call, text=None, thought=False),
        SimpleNamespace(function_call=None, text="hello", thought=False),
        SimpleNamespace(function_call=None, text="thinking", thought=True),
    ]
    response = SimpleNamespace(candidates=[SimpleNamespace(content=SimpleNamespace(parts=parts))])
    turn = parse_response(response)
    assert turn.stop_reason == "tool_use"
    assert turn.tool_calls[0].arguments == {"question": "x"}
    assert turn.text == "hello"


def test_parse_empty_response_raises():
    with pytest.raises(ModelError):
        parse_response(SimpleNamespace(candidates=[]))


class FlakyModels:
    def __init__(self, failures, error):
        self.failures = failures
        self.error = error
        self.calls = 0

    def generate_content(self, model, contents, config):
        self.calls += 1
        if self.calls <= self.failures:
            raise self.error
        part = SimpleNamespace(function_call=None, text="ok", thought=False)
        return SimpleNamespace(candidates=[SimpleNamespace(content=SimpleNamespace(parts=[part]))])


def build_client(failures, code):
    from google.genai import errors

    from app.core.gemini_client import GeminiModelClient

    client = GeminiModelClient("test-key", "m", retry_delay_seconds=0)
    models = FlakyModels(failures, errors.APIError(code, {"error": {"message": "x"}}))
    client._client = SimpleNamespace(models=models)
    return client, models


def test_retryable_status_is_retried_until_success():
    client, models = build_client(failures=2, code=503)
    assert client.generate("s", [Message("user", "hi")], []).text == "ok"
    assert models.calls == 3


def test_retries_stop_after_max_attempts():
    client, models = build_client(failures=5, code=429)
    with pytest.raises(ModelError):
        client.generate("s", [Message("user", "hi")], [])
    assert models.calls == 3


def test_non_retryable_status_fails_immediately():
    client, models = build_client(failures=5, code=400)
    with pytest.raises(ModelError):
        client.generate("s", [Message("user", "hi")], [])
    assert models.calls == 1
