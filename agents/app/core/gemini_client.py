import logging
import time
import uuid
from typing import Any, Iterator, List, Tuple

from google import genai
from google.genai import errors, types

from app.core.messages import Message, ToolCall
from app.core.model_client import ModelClient, ModelError, ModelTurn, StreamItem, TextDelta
from app.core.tools import ToolSpec

logger = logging.getLogger(__name__)

RETRYABLE_STATUS_CODES = {429, 500, 503, 504}


def to_contents(messages: List[Message]) -> List[types.Content]:
    contents: List[types.Content] = []
    for message in messages:
        if message.role == "user":
            contents.append(types.Content(role="user", parts=[types.Part(text=message.text)]))
        elif message.role == "assistant":
            parts = []
            if message.text:
                parts.append(types.Part(text=message.text))
            for call in message.tool_calls:
                parts.append(types.Part(function_call=types.FunctionCall(name=call.name, args=call.arguments)))
            contents.append(types.Content(role="model", parts=parts))
        elif message.role == "tool":
            parts = [
                types.Part.from_function_response(name=result.name, response=result.content)
                for result in message.tool_results
            ]
            contents.append(types.Content(role="user", parts=parts))
    return contents


def to_declarations(tools: List[ToolSpec]) -> List[types.Tool]:
    if not tools:
        return []
    declarations = [
        types.FunctionDeclaration(name=t.name, description=t.description, parameters_json_schema=t.parameters)
        for t in tools
    ]
    return [types.Tool(function_declarations=declarations)]


def parse_parts(parts: Any) -> Tuple[str, List[ToolCall]]:
    text_parts: List[str] = []
    calls: List[ToolCall] = []
    for part in parts or []:
        if part.function_call is not None:
            calls.append(
                ToolCall(
                    id=str(uuid.uuid4()),
                    name=part.function_call.name,
                    arguments=dict(part.function_call.args or {}),
                )
            )
        elif part.text and not getattr(part, "thought", False):
            text_parts.append(part.text)
    return "".join(text_parts), calls


def parse_response(response: Any) -> ModelTurn:
    candidates = getattr(response, "candidates", None)
    if not candidates or candidates[0].content is None:
        raise ModelError("The model returned no content")
    text, calls = parse_parts(candidates[0].content.parts)
    return ModelTurn(text=text, tool_calls=calls)


class GeminiModelClient(ModelClient):
    def __init__(
        self,
        api_key: str,
        model: str,
        inline_system_prompt: bool = False,
        timeout_seconds: int = 60,
        max_attempts: int = 3,
        retry_delay_seconds: float = 3.0,
    ) -> None:
        self._client = genai.Client(api_key=api_key, http_options=types.HttpOptions(timeout=timeout_seconds * 1000))
        self._model = model
        self._inline_system_prompt = inline_system_prompt
        self._max_attempts = max_attempts
        self._retry_delay_seconds = retry_delay_seconds

    def _build_request(
        self, system_prompt: str, messages: List[Message], tools: List[ToolSpec]
    ) -> Tuple[List[types.Content], types.GenerateContentConfig]:
        contents = to_contents(messages)
        system_instruction = system_prompt
        if self._inline_system_prompt:
            contents.insert(0, types.Content(role="user", parts=[types.Part(text=system_prompt)]))
            system_instruction = None
        config = types.GenerateContentConfig(system_instruction=system_instruction, tools=to_declarations(tools) or None)
        return contents, config

    def generate(self, system_prompt: str, messages: List[Message], tools: List[ToolSpec]) -> ModelTurn:
        contents, config = self._build_request(system_prompt, messages, tools)
        return parse_response(self._generate_with_retry(contents, config))

    def stream(self, system_prompt: str, messages: List[Message], tools: List[ToolSpec]) -> Iterator[StreamItem]:
        contents, config = self._build_request(system_prompt, messages, tools)
        for attempt in range(1, self._max_attempts + 1):
            delivered = False
            try:
                text_parts: List[str] = []
                calls: List[ToolCall] = []
                received_content = False
                for chunk in self._client.models.generate_content_stream(
                    model=self._model, contents=contents, config=config
                ):
                    candidates = getattr(chunk, "candidates", None)
                    if not candidates or candidates[0].content is None:
                        continue
                    received_content = True
                    delta, chunk_calls = parse_parts(candidates[0].content.parts)
                    calls.extend(chunk_calls)
                    if delta:
                        delivered = True
                        text_parts.append(delta)
                        yield TextDelta(delta)
                if not received_content:
                    raise ModelError("The model returned no content")
                yield ModelTurn(text="".join(text_parts), tool_calls=calls)
                return
            except errors.APIError as error:
                retryable = error.code in RETRYABLE_STATUS_CODES
                logger.warning("Model stream failed with status %s (attempt %s)", error.code, attempt)
                if delivered or not retryable or attempt == self._max_attempts:
                    raise ModelError(f"The model request failed with status {error.code}") from error
                time.sleep(self._retry_delay_seconds * attempt)
            except ModelError:
                raise
            except Exception as error:
                logger.warning("Model stream failed with %s", type(error).__name__)
                raise ModelError("The model request failed") from error

    def _generate_with_retry(self, contents: List[types.Content], config: types.GenerateContentConfig) -> Any:
        for attempt in range(1, self._max_attempts + 1):
            try:
                return self._client.models.generate_content(model=self._model, contents=contents, config=config)
            except errors.APIError as error:
                retryable = error.code in RETRYABLE_STATUS_CODES
                logger.warning("Model request failed with status %s (attempt %s)", error.code, attempt)
                if not retryable or attempt == self._max_attempts:
                    raise ModelError(f"The model request failed with status {error.code}") from error
                time.sleep(self._retry_delay_seconds * attempt)
            except Exception as error:
                logger.warning("Model request failed with %s", type(error).__name__)
                raise ModelError("The model request failed") from error
        raise ModelError("The model request failed")
