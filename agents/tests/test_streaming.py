import json
from types import SimpleNamespace

import pytest
from fastapi.testclient import TestClient
from google.genai import errors

from app.agents.docs_reader import DocsReaderAgent
from app.agents.orchestrator import OrchestratorAgent
from app.api.chat import get_orchestrator
from app.core.agent_loop import AgentResult, stream_agent
from app.core.conversation_store import InMemoryConversationStore
from app.core.gemini_client import GeminiModelClient
from app.core.messages import Message
from app.core.model_client import ModelClient, ModelError, ModelTurn, TextDelta
from app.core.tools import ToolRegistry
from app.main import app
from tests.fakes import ScriptedModel, final_turn, tool_turn


class StreamingModel(ModelClient):
    def __init__(self, turns):
        self._turns = list(turns)

    def generate(self, system_prompt, messages, tools):
        raise AssertionError("generate must not be used when streaming")

    def stream(self, system_prompt, messages, tools):
        pieces, turn = self._turns.pop(0)
        for piece in pieces:
            yield TextDelta(piece)
        yield turn


def collect(iterator):
    deltas, results = [], []
    for item in iterator:
        (deltas if isinstance(item, TextDelta) else results).append(item)
    return [d.text for d in deltas], results


def test_deltas_are_streamed_as_they_arrive():
    model = StreamingModel([(["Hel", "lo ", "there"], final_turn("Hello there"))])
    deltas, results = collect(stream_agent(model, "s", [Message("user", "hi")], ToolRegistry([])))
    assert deltas == ["Hel", "lo ", "there"]
    assert isinstance(results[0], AgentResult)
    assert results[0].text == "Hello there"


def test_turns_with_text_before_and_after_a_tool_are_separated():
    model = StreamingModel([
        (["Let me check."], tool_turn("missing")),
        (["Here ", "you go."], final_turn("Here you go.")),
    ])
    deltas, _ = collect(stream_agent(model, "s", [Message("user", "hi")], ToolRegistry([])))
    assert "".join(deltas) == "Let me check.\n\nHere you go."


def test_silent_tool_turn_adds_no_separator():
    model = StreamingModel([([], tool_turn("missing")), (["Answer"], final_turn("Answer"))])
    deltas, _ = collect(stream_agent(model, "s", [Message("user", "hi")], ToolRegistry([])))
    assert deltas == ["Answer"]


def test_default_stream_falls_back_to_a_single_delta():
    model = ScriptedModel([final_turn("whole answer")])
    deltas, results = collect(stream_agent(model, "s", [Message("user", "hi")], ToolRegistry([])))
    assert deltas == ["whole answer"]
    assert results[0].text == "whole answer"


def test_orchestrator_stream_persists_history_after_completion(doc_file):
    model = StreamingModel([(["Hi", "!"], final_turn("Hi!"))])
    store = InMemoryConversationStore()
    orchestrator = OrchestratorAgent(model, store, DocsReaderAgent(ScriptedModel([]), doc_file))
    deltas, _ = collect(orchestrator.handle_stream("c1", "hello"))
    assert deltas == ["Hi", "!"]
    assert [m.role for m in store.get("c1")] == ["user", "assistant"]


def parse_sse(body):
    events = []
    for block in body.strip().split("\n\n"):
        lines = dict(line.split(": ", 1) for line in block.split("\n"))
        events.append((lines["event"], json.loads(lines["data"])))
    return events


@pytest.fixture
def client():
    yield TestClient(app)
    app.dependency_overrides.clear()


def override(model, doc_file):
    orchestrator = OrchestratorAgent(model, InMemoryConversationStore(), DocsReaderAgent(ScriptedModel([]), doc_file))
    app.dependency_overrides[get_orchestrator] = lambda: orchestrator


def test_chat_stream_endpoint_sends_delta_and_done_events(client, doc_file):
    override(StreamingModel([(["Hello ", "Ukraine: привіт"], final_turn("x"))]), doc_file)
    response = client.post("/chat/stream", json={"conversation_id": "c1", "message": "hi"})
    assert response.headers["content-type"].startswith("text/event-stream")
    events = parse_sse(response.text)
    assert events[0] == ("delta", {"text": "Hello "})
    assert events[1] == ("delta", {"text": "Ukraine: привіт"})
    assert events[-1] == ("done", {"conversation_id": "c1"})


def test_chat_stream_reports_model_failure_as_error_event(client, doc_file):
    class Failing(StreamingModel):
        def stream(self, system_prompt, messages, tools):
            raise ModelError("api key abc was rejected")
            yield

    override(Failing([]), doc_file)
    response = client.post("/chat/stream", json={"message": "hi"})
    events = parse_sse(response.text)
    assert events == [("error", {"message": "The agent could not produce an answer"})]
    assert "abc" not in response.text


def chunk(text=None, call=None):
    part = SimpleNamespace(
        function_call=call, text=text, thought=False
    )
    return SimpleNamespace(candidates=[SimpleNamespace(content=SimpleNamespace(parts=[part]))])


class FakeStreamingModels:
    def __init__(self, attempts):
        self.attempts = list(attempts)
        self.calls = 0

    def generate_content_stream(self, model, contents, config):
        self.calls += 1
        behavior = self.attempts.pop(0)
        for item in behavior:
            if isinstance(item, Exception):
                raise item
            yield item


def gemini_with(attempts):
    client = GeminiModelClient("test-key", "m", retry_delay_seconds=0)
    models = FakeStreamingModels(attempts)
    client._client = SimpleNamespace(models=models)
    return client, models


def api_error(code):
    return errors.APIError(code, {"error": {"message": "x"}})


def test_gemini_stream_yields_text_chunks_then_final_turn():
    client, _ = gemini_with([[chunk("Hel"), chunk("lo")]])
    items = list(client.stream("s", [Message("user", "hi")], []))
    assert [i.text for i in items[:-1]] == ["Hel", "lo"]
    assert items[-1].text == "Hello"
    assert items[-1].stop_reason == "end_turn"


def test_gemini_stream_collects_function_calls():
    call = SimpleNamespace(name="ask_help_guide", args={"question": "q"})
    client, _ = gemini_with([[chunk(call=call)]])
    items = list(client.stream("s", [Message("user", "hi")], []))
    assert items[-1].stop_reason == "tool_use"
    assert items[-1].tool_calls[0].arguments == {"question": "q"}


def test_gemini_stream_retries_when_nothing_was_delivered():
    client, models = gemini_with([[api_error(503)], [chunk("ok")]])
    items = list(client.stream("s", [Message("user", "hi")], []))
    assert items[-1].text == "ok"
    assert models.calls == 2


def test_gemini_stream_does_not_retry_after_text_was_delivered():
    client, models = gemini_with([[chunk("partial"), api_error(503)], [chunk("again")]])
    received = []
    with pytest.raises(ModelError):
        for item in client.stream("s", [Message("user", "hi")], []):
            received.append(item)
    assert [i.text for i in received] == ["partial"]
    assert models.calls == 1


def test_gemini_stream_with_no_content_raises():
    client, _ = gemini_with([[SimpleNamespace(candidates=[])]])
    with pytest.raises(ModelError):
        list(client.stream("s", [Message("user", "hi")], []))
