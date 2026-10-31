import pytest
from fastapi.testclient import TestClient

from app.agents.docs_reader import DocsReaderAgent
from app.agents.orchestrator import OrchestratorAgent
from app.api.chat import get_orchestrator
from app.config import ConfigurationError, Settings
from app.core.conversation_store import InMemoryConversationStore
from app.core.model_client import ModelError
from app.main import app
from tests.fakes import ScriptedModel, final_turn


@pytest.fixture
def client():
    yield TestClient(app)
    app.dependency_overrides.clear()


def override(model, doc_file):
    orchestrator = OrchestratorAgent(model, InMemoryConversationStore(), DocsReaderAgent(model, doc_file))
    app.dependency_overrides[get_orchestrator] = lambda: orchestrator


def test_chat_returns_reply_and_conversation_id(client, doc_file):
    override(ScriptedModel([final_turn("hi there")]), doc_file)
    response = client.post("/chat", json={"message": "hello"})
    assert response.status_code == 200
    body = response.json()
    assert body["reply"] == "hi there"
    assert body["conversation_id"]


def test_chat_reuses_conversation_id(client, doc_file):
    model = ScriptedModel([final_turn("one"), final_turn("two")])
    override(model, doc_file)
    first = client.post("/chat", json={"message": "a"}).json()
    client.post("/chat", json={"conversation_id": first["conversation_id"], "message": "b"})
    assert [m.text for m in model.calls[1][1] if m.text] == ["a", "one", "b"]


def test_empty_message_is_rejected(client, doc_file):
    override(ScriptedModel([]), doc_file)
    assert client.post("/chat", json={"message": ""}).status_code == 422


def test_model_failure_returns_502_without_details(client, doc_file):
    class Failing(ScriptedModel):
        def generate(self, system_prompt, messages, tools):
            raise ModelError("api key abc was rejected")

    override(Failing([]), doc_file)
    response = client.post("/chat", json={"message": "hello"})
    assert response.status_code == 502
    assert "abc" not in response.text


def test_missing_configuration_returns_503(client):
    def not_configured():
        raise ConfigurationError("GEMINI_API_KEY is not set")

    app.dependency_overrides[get_orchestrator] = not_configured
    response = client.post("/chat", json={"message": "hello"})
    assert response.status_code == 503
    assert "GEMINI" not in response.text


def test_health_works_without_configuration(client):
    assert client.get("/health").json() == {"status": "ok"}


def test_settings_require_key_and_model():
    with pytest.raises(ConfigurationError):
        Settings(gemini_api_key=None, gemini_model="m", _env_file=None).require_model_access()
    with pytest.raises(ConfigurationError):
        Settings(gemini_api_key="k", gemini_model="", _env_file=None).require_model_access()
    Settings(gemini_api_key="k", gemini_model="m", _env_file=None).require_model_access()


def test_chat_passes_the_user_to_the_case_facts(client, doc_file):
    orchestrator = OrchestratorAgent(
        ScriptedModel([final_turn("hi Mia")]), InMemoryConversationStore(), DocsReaderAgent(ScriptedModel([]), doc_file)
    )
    app.dependency_overrides[get_orchestrator] = lambda: orchestrator
    response = client.post(
        "/chat",
        json={"conversation_id": "c1", "message": "hello", "user": {"id": "student-1", "name": "Mia", "age": 11}},
    )
    assert response.status_code == 200
    student = orchestrator.facts_for("c1").student
    assert (student.id, student.name, student.age) == ("student-1", "Mia", 11)


def test_chat_stream_passes_the_user_to_the_case_facts(client, doc_file):
    orchestrator = OrchestratorAgent(
        ScriptedModel([final_turn("hi")]), InMemoryConversationStore(), DocsReaderAgent(ScriptedModel([]), doc_file)
    )
    app.dependency_overrides[get_orchestrator] = lambda: orchestrator
    client.post("/chat/stream", json={"conversation_id": "c2", "message": "hi", "user": {"id": "student-7"}})
    assert orchestrator.facts_for("c2").student.id == "student-7"


def test_chat_rejects_a_user_without_id(client, doc_file):
    override(ScriptedModel([]), doc_file)
    assert client.post("/chat", json={"message": "hi", "user": {"name": "x"}}).status_code == 422
