import json

import httpx
import pytest

from app.core.tools import ToolError
from app.tools.backend_api import HttpBackendApi


def api_with(handler):
    requests = []

    def recording(request):
        requests.append(request)
        return handler(request)

    return HttpBackendApi("http://api.test/", "agent-key-for-tests", transport=httpx.MockTransport(recording)), requests


def test_context_request_sends_agent_key_and_path():
    api, requests = api_with(lambda request: httpx.Response(200, json={"name": "Mia", "teachers": []}))
    assert api.get_student_context("s1") == {"name": "Mia", "teachers": []}
    assert requests[0].method == "GET"
    assert requests[0].url.path == "/api/internal/students/s1/context"
    assert requests[0].headers["X-Agent-Key"] == "agent-key-for-tests"


def test_cancel_posts_to_the_cancel_endpoint():
    api, requests = api_with(lambda request: httpx.Response(204))
    assert api.cancel_subscription("s1") == {}
    assert requests[0].method == "POST"
    assert requests[0].url.path == "/api/internal/students/s1/subscription/cancel"


def test_concern_body_uses_the_api_contract():
    api, requests = api_with(lambda request: httpx.Response(201, json={"id": "c1"}))
    api.create_concern("s1", "t1", "conv", "text", "Safety", "Critical", "summary")
    assert requests[0].url.path == "/api/internal/concerns"
    assert json.loads(requests[0].content) == {
        "studentId": "s1",
        "teacherId": "t1",
        "conversationId": "conv",
        "text": "text",
        "category": "Safety",
        "severity": "Critical",
        "summary": "summary",
    }


@pytest.mark.parametrize(
    "status,category,retryable",
    [
        (400, "invalid_input", False),
        (401, "forbidden", False),
        (403, "forbidden", False),
        (404, "not_found", False),
        (409, "conflict", False),
        (500, "backend_unavailable", True),
        (503, "backend_unavailable", True),
    ],
)
def test_http_errors_map_to_tool_errors(status, category, retryable):
    api, _ = api_with(lambda request: httpx.Response(status, json={"detail": "secret internals"}))
    with pytest.raises(ToolError) as raised:
        api.get_student_context("s1")
    assert raised.value.category == category
    assert raised.value.retryable is retryable
    assert "secret" not in raised.value.message


def test_connection_failure_is_retryable():
    def refuse(request):
        raise httpx.ConnectError("refused")

    api, _ = api_with(refuse)
    with pytest.raises(ToolError) as raised:
        api.cancel_subscription("s1")
    assert raised.value.category == "backend_unavailable"
    assert raised.value.retryable
