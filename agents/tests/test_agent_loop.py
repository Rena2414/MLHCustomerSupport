import pytest

from app.core.agent_loop import AgentLoopError, run_agent
from app.core.messages import Message
from app.core.tools import Tool, ToolError, ToolRegistry, ToolSpec
from tests.fakes import ScriptedModel, final_turn, tool_turn


def echo_tool() -> Tool:
    spec = ToolSpec(name="echo", description="Echoes", parameters={"type": "object", "properties": {}})
    return Tool(spec=spec, handler=lambda arguments: {"echo": arguments.get("value")})


def failing_tool(error: Exception) -> Tool:
    spec = ToolSpec(name="fail", description="Fails", parameters={"type": "object", "properties": {}})

    def handler(arguments):
        raise error

    return Tool(spec=spec, handler=handler)


def test_loop_continues_on_tool_use_and_stops_on_end_turn():
    model = ScriptedModel([tool_turn("echo", value="a"), tool_turn("echo", value="b"), final_turn("done")])
    result = run_agent(model, "system", [Message("user", "hi")], ToolRegistry([echo_tool()]))
    assert result.text == "done"
    assert len(model.calls) == 3
    roles = [m.role for m in result.new_messages]
    assert roles == ["assistant", "tool", "assistant", "tool", "assistant"]


def test_tool_results_are_sent_back_to_the_model():
    model = ScriptedModel([tool_turn("echo", value="a"), final_turn("done")])
    run_agent(model, "system", [Message("user", "hi")], ToolRegistry([echo_tool()]))
    last_messages = model.calls[1][1]
    assert last_messages[-1].tool_results[0].content == {"echo": "a"}


def test_unknown_tool_returns_structured_error():
    registry = ToolRegistry([echo_tool()])
    model = ScriptedModel([tool_turn("missing"), final_turn("done")])
    result = run_agent(model, "system", [Message("user", "hi")], registry)
    payload = result.new_messages[1].tool_results[0]
    assert payload.is_error
    assert payload.content["error"]["category"] == "unknown_tool"
    assert payload.content["error"]["retryable"] is False


def test_tool_error_keeps_category_and_retryable_flag():
    registry = ToolRegistry([failing_tool(ToolError("io_error", "disk", retryable=True))])
    model = ScriptedModel([tool_turn("fail"), final_turn("done")])
    result = run_agent(model, "system", [Message("user", "hi")], registry)
    error = result.new_messages[1].tool_results[0].content["error"]
    assert error == {"category": "io_error", "retryable": True, "message": "disk"}


def test_unexpected_exception_does_not_leak_details():
    registry = ToolRegistry([failing_tool(RuntimeError("secret internals"))])
    model = ScriptedModel([tool_turn("fail"), final_turn("done")])
    result = run_agent(model, "system", [Message("user", "hi")], registry)
    error = result.new_messages[1].tool_results[0].content["error"]
    assert error["category"] == "internal"
    assert "secret" not in error["message"]


def test_safety_cap_raises_when_model_never_finishes():
    model = ScriptedModel([tool_turn("echo") for _ in range(5)])
    with pytest.raises(AgentLoopError):
        run_agent(model, "system", [Message("user", "hi")], ToolRegistry([echo_tool()]), max_iterations=3)
