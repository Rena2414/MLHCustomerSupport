import json
import uuid
from functools import lru_cache
from typing import Iterator, Optional

from fastapi import APIRouter, Depends, HTTPException
from fastapi.responses import StreamingResponse
from pydantic import BaseModel, Field

from app.agents.docs_reader import DocsReaderAgent
from app.agents.orchestrator import OrchestratorAgent
from app.config import Settings
from app.core.agent_loop import AgentLoopError
from app.core.conversation_store import InMemoryConversationStore
from app.core.gemini_client import GeminiModelClient
from app.core.model_client import ModelError, TextDelta

router = APIRouter()


class ChatRequest(BaseModel):
    conversation_id: Optional[str] = None
    message: str = Field(min_length=1, max_length=4000)


class ChatResponse(BaseModel):
    conversation_id: str
    reply: str


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    return Settings()


@lru_cache(maxsize=1)
def get_orchestrator() -> OrchestratorAgent:
    settings = get_settings()
    settings.require_model_access()
    model = GeminiModelClient(
        api_key=settings.gemini_api_key.get_secret_value(),
        model=settings.gemini_model,
        inline_system_prompt=settings.inline_system_prompt,
    )
    docs_reader = DocsReaderAgent(model, settings.system_doc_path, settings.max_iterations)
    return OrchestratorAgent(model, InMemoryConversationStore(), docs_reader, settings.max_iterations)


@router.post("/chat", response_model=ChatResponse)
def chat(request: ChatRequest, orchestrator: OrchestratorAgent = Depends(get_orchestrator)) -> ChatResponse:
    conversation_id = request.conversation_id or str(uuid.uuid4())
    try:
        reply = orchestrator.handle(conversation_id, request.message)
    except (ModelError, AgentLoopError):
        raise HTTPException(status_code=502, detail="The agent could not produce an answer")
    return ChatResponse(conversation_id=conversation_id, reply=reply)


def format_event(name: str, data: dict) -> str:
    return f"event: {name}\ndata: {json.dumps(data)}\n\n"


@router.post("/chat/stream")
def chat_stream(request: ChatRequest, orchestrator: OrchestratorAgent = Depends(get_orchestrator)) -> StreamingResponse:
    conversation_id = request.conversation_id or str(uuid.uuid4())

    def events() -> Iterator[str]:
        try:
            for item in orchestrator.handle_stream(conversation_id, request.message):
                if isinstance(item, TextDelta):
                    yield format_event("delta", {"text": item.text})
            yield format_event("done", {"conversation_id": conversation_id})
        except (ModelError, AgentLoopError):
            yield format_event("error", {"message": "The agent could not produce an answer"})

    return StreamingResponse(
        events(),
        media_type="text/event-stream",
        headers={"Cache-Control": "no-cache", "X-Accel-Buffering": "no"},
    )
