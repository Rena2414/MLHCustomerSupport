from abc import ABC, abstractmethod
from typing import Any, Dict, Optional

import httpx

from app.core.tools import ToolError

AGENT_KEY_HEADER = "X-Agent-Key"


class BackendApi(ABC):
    @abstractmethod
    def get_student_context(self, student_id: str) -> Dict[str, Any]:
        ...

    @abstractmethod
    def cancel_subscription(self, student_id: str) -> Dict[str, Any]:
        ...

    @abstractmethod
    def create_concern(
        self,
        student_id: str,
        teacher_id: str,
        conversation_id: Optional[str],
        text: str,
        category: str,
        severity: str,
        summary: str,
    ) -> Dict[str, Any]:
        ...


def error_from_response(response: httpx.Response) -> ToolError:
    status = response.status_code
    if status in (400, 422):
        return ToolError("invalid_input", "The request was rejected as invalid")
    if status in (401, 403):
        return ToolError("forbidden", "This action is not allowed")
    if status == 404:
        return ToolError("not_found", "The requested record was not found")
    if status == 409:
        return ToolError("conflict", "The request conflicts with the current state")
    if status == 429 or status >= 500:
        return ToolError("backend_unavailable", "The school system is not available right now", retryable=True)
    return ToolError("backend_error", f"The school system answered with status {status}")


class HttpBackendApi(BackendApi):
    def __init__(
        self,
        base_url: str,
        agent_key: str,
        timeout_seconds: float = 10.0,
        transport: Optional[httpx.BaseTransport] = None,
    ) -> None:
        self._client = httpx.Client(
            base_url=base_url.rstrip("/"),
            headers={AGENT_KEY_HEADER: agent_key},
            timeout=timeout_seconds,
            transport=transport,
        )

    def get_student_context(self, student_id: str) -> Dict[str, Any]:
        return self._send("GET", f"/api/internal/students/{student_id}/context")

    def cancel_subscription(self, student_id: str) -> Dict[str, Any]:
        return self._send("POST", f"/api/internal/students/{student_id}/subscription/cancel")

    def create_concern(
        self,
        student_id: str,
        teacher_id: str,
        conversation_id: Optional[str],
        text: str,
        category: str,
        severity: str,
        summary: str,
    ) -> Dict[str, Any]:
        body = {
            "studentId": student_id,
            "teacherId": teacher_id,
            "conversationId": conversation_id,
            "text": text,
            "category": category,
            "severity": severity,
            "summary": summary,
        }
        return self._send("POST", "/api/internal/concerns", body)

    def _send(self, method: str, path: str, body: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        try:
            response = self._client.request(method, path, json=body)
        except httpx.TimeoutException:
            raise ToolError("backend_unavailable", "The school system took too long to answer", retryable=True)
        except httpx.HTTPError:
            raise ToolError("backend_unavailable", "The school system could not be reached", retryable=True)
        if response.status_code >= 400:
            raise error_from_response(response)
        if not response.content:
            return {}
        try:
            payload = response.json()
        except ValueError:
            raise ToolError("backend_error", "The school system sent an unreadable answer")
        return payload if isinstance(payload, dict) else {"items": payload}
