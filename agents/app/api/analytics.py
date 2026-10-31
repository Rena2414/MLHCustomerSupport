from typing import List

from fastapi import APIRouter, Depends, HTTPException
from pydantic import BaseModel, Field

from app.agents.teacher_summary import ConcernRecord, TeacherSummarizer
from app.api.dependencies import get_teacher_summarizer
from app.core.json_output import JsonOutputError
from app.core.model_client import ModelError

router = APIRouter(prefix="/analytics")


class ConcernItem(BaseModel):
    text: str = Field(max_length=4000)
    category: str = Field(max_length=50)
    severity: str = Field(max_length=50)
    created_at: str = Field(default="", max_length=50)


class TeacherSummaryRequest(BaseModel):
    teacher_name: str = Field(min_length=1, max_length=200)
    concerns: List[ConcernItem] = Field(default_factory=list, max_length=500)


class TeacherSummaryResponse(BaseModel):
    summary: str
    recommended_actions: List[str]


@router.post("/teacher-summary", response_model=TeacherSummaryResponse)
def teacher_summary(
    request: TeacherSummaryRequest, summarizer: TeacherSummarizer = Depends(get_teacher_summarizer)
) -> TeacherSummaryResponse:
    records = [ConcernRecord(c.text, c.category, c.severity, c.created_at) for c in request.concerns]
    try:
        result = summarizer.summarize(request.teacher_name, records)
    except (ModelError, JsonOutputError):
        raise HTTPException(status_code=502, detail="The summary could not be produced")
    return TeacherSummaryResponse(summary=result.summary, recommended_actions=result.recommended_actions)
