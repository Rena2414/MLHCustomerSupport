from dataclasses import dataclass
from typing import Iterator, List, Union

from app.core.messages import Message
from app.core.model_client import ModelClient, ModelTurn, TextDelta
from app.core.tools import ToolRegistry

TURN_SEPARATOR = "\n\n"


class AgentLoopError(Exception):
    pass


@dataclass
class AgentResult:
    text: str
    new_messages: List[Message]


def stream_agent(
    model: ModelClient,
    system_prompt: str,
    history: List[Message],
    registry: ToolRegistry,
    max_iterations: int = 25,
) -> Iterator[Union[TextDelta, AgentResult]]:
    working = list(history)
    new_messages: List[Message] = []
    streamed_any = False
    for _ in range(max_iterations):
        turn = None
        turn_started = False
        for item in model.stream(system_prompt, working, registry.specs()):
            if isinstance(item, TextDelta):
                if not item.text:
                    continue
                if streamed_any and not turn_started:
                    yield TextDelta(TURN_SEPARATOR)
                turn_started = True
                streamed_any = True
                yield item
            elif isinstance(item, ModelTurn):
                turn = item
        if turn is None:
            raise AgentLoopError("The model ended the stream without a final turn")
        assistant = Message(role="assistant", text=turn.text, tool_calls=turn.tool_calls)
        working.append(assistant)
        new_messages.append(assistant)
        if turn.stop_reason == "end_turn":
            yield AgentResult(text=turn.text, new_messages=new_messages)
            return
        results = [registry.execute(call) for call in turn.tool_calls]
        tool_message = Message(role="tool", tool_results=results)
        working.append(tool_message)
        new_messages.append(tool_message)
    raise AgentLoopError(f"Agent did not finish within {max_iterations} iterations")


def run_agent(
    model: ModelClient,
    system_prompt: str,
    history: List[Message],
    registry: ToolRegistry,
    max_iterations: int = 25,
) -> AgentResult:
    for item in stream_agent(model, system_prompt, history, registry, max_iterations):
        if isinstance(item, AgentResult):
            return item
    raise AgentLoopError("The agent finished without a result")
