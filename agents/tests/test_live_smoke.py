import pytest

from app.agents.docs_reader import DocsReaderAgent
from app.agents.orchestrator import OrchestratorAgent
from app.config import Settings
from app.core.conversation_store import InMemoryConversationStore
from app.core.gemini_client import GeminiModelClient

pytestmark = pytest.mark.live


@pytest.mark.skipif(
    not (Settings().gemini_api_key and Settings().gemini_model), reason="model access not configured"
)
def test_orchestrator_answers_from_system_doc():
    settings = Settings()
    model = GeminiModelClient(
        settings.gemini_api_key.get_secret_value(), settings.gemini_model, settings.inline_system_prompt
    )
    orchestrator = OrchestratorAgent(
        model, InMemoryConversationStore(), DocsReaderAgent(model, settings.system_doc_path)
    )
    reply = orchestrator.handle("smoke", "How do I join a class?")
    assert "login" in reply.lower()
