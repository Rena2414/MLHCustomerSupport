from app.agents.concern_classifier import ConcernClassifier
from app.core.model_client import ModelError
from tests.fakes import ScriptedModel, final_turn


def classify(text):
    return ConcernClassifier(ScriptedModel([final_turn(text)])).classify("concern")


def test_parses_json_inside_code_fences():
    result = classify('```json\n{"category": "Fairness", "severity": "Low", "summary": "Unfair grade."}\n```')
    assert (result.category, result.severity, result.summary) == ("Fairness", "Low", "Unfair grade.")
    assert not result.needs_trusted_adult


def test_normalizes_spelling_of_categories():
    result = classify('{"category": "Teaching quality", "severity": "medium", "summary": "s"}')
    assert (result.category, result.severity) == ("TeachingQuality", "Medium")


def test_unknown_category_falls_back_to_other_high():
    result = classify('{"category": "Weather", "severity": "Low", "summary": "s"}')
    assert (result.category, result.severity) == ("Other", "High")


def test_invalid_json_falls_back_to_other_high_with_the_text():
    result = ConcernClassifier(ScriptedModel([final_turn("not json")])).classify("the teacher yells")
    assert (result.category, result.severity, result.summary) == ("Other", "High", "the teacher yells")


def test_model_failure_falls_back():
    class Failing(ScriptedModel):
        def generate(self, system_prompt, messages, tools):
            raise ModelError("down")

    assert ConcernClassifier(Failing([])).classify("x").category == "Other"


def test_safety_and_critical_need_a_trusted_adult():
    assert classify('{"category": "Safety", "severity": "High", "summary": "s"}').needs_trusted_adult
    assert classify('{"category": "Behavior", "severity": "Critical", "summary": "s"}').needs_trusted_adult
