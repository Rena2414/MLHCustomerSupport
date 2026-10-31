import json
import uuid
from typing import Iterator, Optional

from fastapi import APIRouter, Depends, HTTPException
from fastapi.responses import StreamingResponse
from pydantic import BaseModel, Field

from app.agents.orchestrator import OrchestratorAgent
from app.api.dependencies import get_orchestrator
from app.core.agent_loop import AgentLoopError
from app.core.case_facts import StudentProfile
from app.core.model_client import ModelError, TextDelta

router = APIRouter()


class ChatUser(BaseModel):
    id: str = Field(min_length=1, max_length=100)
    name: str = Field(default="", max_length=200)
    age: Optional[int] = Field(default=None, ge=0, le=150)

    def to_profile(self) -> StudentProfile:
        return StudentProfile(id=self.id, name=self.name, age=self.age)


class ChatRequest(BaseModel):
    conversation_id: Optional[str] = None
    message: str = Field(min_length=1, max_length=4000)
    user: Optional[ChatUser] = None

    def student(self) -> Optional[StudentProfile]:
        return self.user.to_profile() if self.user else None


class ChatResponse(BaseModel):
    conversation_id: str
    reply: str


@router.post("/chat", response_model=ChatResponse)
def chat(request: ChatRequest, orchestrator: OrchestratorAgent = Depends(get_orchestrator)) -> ChatResponse:
    conversation_id = request.conversation_id or str(uuid.uuid4())
    try:
        reply = orchestrator.handle(conversation_id, request.message, request.student())
    except (ModelError, AgentLoopError):
        raise HTTPException(status_code=502, detail="The agent could not produce an answer")
    return ChatResponse(conversation_id=conversation_id, reply=reply)


def format_event(name: str, data: dict) -> str:
    return f"event: {name}\ndata: {json.dumps(data)}\n\n"


@router.post("/chat/stream")
def chat_stream(request: ChatRequest, orchestrator: OrchestratorAgent = Depends(get_orchestrator)) -> StreamingResponse:
    conversation_id = request.conversation_id or str(uuid.uuid4())
    student = request.student()

    def events() -> Iterator[str]:
        try:
            for item in orchestrator.handle_stream(conversation_id, request.message, student):
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
