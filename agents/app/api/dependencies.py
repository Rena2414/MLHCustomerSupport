from functools import lru_cache
from typing import Optional

from app.agents.concern_classifier import ConcernClassifier
from app.agents.docs_reader import DocsReaderAgent
from app.agents.orchestrator import OrchestratorAgent
from app.agents.teacher_summary import TeacherSummarizer
from app.config import Settings
from app.core.conversation_store import InMemoryConversationStore
from app.core.gemini_client import GeminiModelClient
from app.core.model_client import ModelClient
from app.tools.backend_api import BackendApi, HttpBackendApi


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    return Settings()


@lru_cache(maxsize=1)
def get_model() -> ModelClient:
    settings = get_settings()
    settings.require_model_access()
    return GeminiModelClient(
        api_key=settings.gemini_api_key.get_secret_value(),
        model=settings.gemini_model,
        inline_system_prompt=settings.inline_system_prompt,
    )


def build_backend(settings: Settings) -> Optional[BackendApi]:
    if settings.agent_api_key is None or not settings.agent_api_key.get_secret_value():
        return None
    return HttpBackendApi(
        base_url=settings.api_base_url,
        agent_key=settings.agent_api_key.get_secret_value(),
        timeout_seconds=settings.api_timeout_seconds,
    )


@lru_cache(maxsize=1)
def get_orchestrator() -> OrchestratorAgent:
    settings = get_settings()
    model = get_model()
    docs_reader = DocsReaderAgent(model, settings.system_doc_path, settings.max_iterations)
    return OrchestratorAgent(
        model,
        InMemoryConversationStore(),
        docs_reader,
        settings.max_iterations,
        backend=build_backend(settings),
        classifier=ConcernClassifier(model),
    )


@lru_cache(maxsize=1)
def get_teacher_summarizer() -> TeacherSummarizer:
    return TeacherSummarizer(get_model())
