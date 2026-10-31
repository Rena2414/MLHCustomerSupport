import json

import pytest
from fastapi.testclient import TestClient

from app.agents.teacher_summary import TeacherSummarizer
from app.api.dependencies import get_teacher_summarizer
from app.core.prompts import load_prompt
from app.main import app
from tests.fakes import ScriptedModel, final_turn

CONCERNS = [
    {"text": "He is late", "category": "LatenessOrAbsence", "severity": "Medium", "created_at": "2026-10-01T10:00:00Z"},
    {"text": "He shouted", "category": "Behavior", "severity": "High", "created_at": "2026-10-20T10:00:00Z"},
]


@pytest.fixture
def client():
    yield TestClient(app)
    app.dependency_overrides.clear()


def override(model):
    summarizer = TeacherSummarizer(model)
    app.dependency_overrides[get_teacher_summarizer] = lambda: summarizer


def test_teacher_summary_returns_summary_and_actions(client):
    model = ScriptedModel(
        [final_turn('{"summary": "Two concerns.", "recommended_actions": ["Talk with the teacher", "Observe a lesson"]}')]
    )
    override(model)
    response = client.post("/analytics/teacher-summary", json={"teacher_name": "Mr Brown", "concerns": CONCERNS})
    assert response.status_code == 200
    assert response.json() == {
        "summary": "Two concerns.",
        "recommended_actions": ["Talk with the teacher", "Observe a lesson"],
    }
    system_prompt, messages, tools = model.calls[0]
    assert system_prompt == load_prompt("teacher_summary")
    assert tools == []
    assert json.loads(messages[0].text)["concerns"][1]["severity"] == "High"


def test_teacher_summary_without_concerns_skips_the_model(client):
    override(ScriptedModel([]))
    response = client.post("/analytics/teacher-summary", json={"teacher_name": "Ms Green", "concerns": []})
    assert response.status_code == 200
    assert response.json()["recommended_actions"] == []


def test_teacher_summary_with_invalid_model_output_returns_502(client):
    override(ScriptedModel([final_turn("no json here")]))
    response = client.post("/analytics/teacher-summary", json={"teacher_name": "Mr Brown", "concerns": CONCERNS})
    assert response.status_code == 502


def test_teacher_summary_requires_a_teacher_name(client):
    override(ScriptedModel([]))
    assert client.post("/analytics/teacher-summary", json={"teacher_name": "", "concerns": []}).status_code == 422
