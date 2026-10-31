from app.agents.docs_reader import DocsReaderAgent
from app.agents.orchestrator import OrchestratorAgent
from app.core.conversation_store import InMemoryConversationStore
from app.core.prompts import load_prompt
from tests.fakes import ScriptedModel, final_turn, tool_turn


def build(doc_file, turns):
    model = ScriptedModel(turns)
    store = InMemoryConversationStore()
    orchestrator = OrchestratorAgent(model, store, DocsReaderAgent(model, doc_file))
    return model, store, orchestrator


def orchestrator_calls(model):
    prompt = load_prompt("orchestrator")
    return [call for call in model.calls if call[0] == prompt]


def test_subagent_reads_file_and_orchestrator_gets_only_the_summary(doc_file):
    model, store, orchestrator = build(
        doc_file,
        [
            tool_turn("ask_help_guide", question="How do I cancel my subscription?"),
            tool_turn("read_help_guide"),
            final_turn("The frontend runs on port 4200."),
            final_turn("The Angular frontend uses port 4200."),
        ],
    )
    reply = orchestrator.handle("c1", "Which port is the frontend on?")
    assert reply == "The Angular frontend uses port 4200."
    for _, messages, _ in orchestrator_calls(model):
        for message in messages:
            assert "UNIQUE_DOC_MARKER_123" not in message.text
            for result in message.tool_results:
                assert "UNIQUE_DOC_MARKER_123" not in str(result.content)
    subagent_calls = [call for call in model.calls if call[0] == load_prompt("docs_reader")]
    assert any("UNIQUE_DOC_MARKER_123" in str(m.tool_results) for _, msgs, _ in subagent_calls for m in msgs)


def test_orchestrator_and_subagent_have_separate_toolsets(doc_file):
    model, _, orchestrator = build(
        doc_file,
        [
            tool_turn("ask_help_guide", question="q"),
            tool_turn("read_help_guide"),
            final_turn("summary"),
            final_turn("answer"),
        ],
    )
    orchestrator.handle("c1", "hello")
    assert model.calls[0][2] == ["ask_help_guide"]
    assert model.calls[1][2] == ["read_help_guide"]


def test_full_history_is_sent_on_every_turn(doc_file):
    model, store, orchestrator = build(
        doc_file,
        [
            tool_turn("ask_help_guide", question="q1"),
            tool_turn("read_help_guide"),
            final_turn("summary one"),
            final_turn("answer one"),
            final_turn("answer two"),
        ],
    )
    orchestrator.handle("c1", "first question")
    orchestrator.handle("c1", "and the second one?")
    second_turn_messages = orchestrator_calls(model)[-1][1]
    texts = [m.text for m in second_turn_messages if m.text]
    assert texts == ["first question", "answer one", "and the second one?"]
    assert any(m.tool_results for m in second_turn_messages)
    assert len(store.get("c1")) == 6


def test_conversations_do_not_share_history(doc_file):
    model, store, orchestrator = build(doc_file, [final_turn("a"), final_turn("b")])
    orchestrator.handle("c1", "one")
    orchestrator.handle("c2", "two")
    assert [m.text for m in orchestrator_calls(model)[1][1]] == ["two"]


def test_user_message_is_kept_when_the_loop_fails(doc_file):
    class Boom(ScriptedModel):
        def generate(self, system_prompt, messages, tools):
            raise RuntimeError("down")

    store = InMemoryConversationStore()
    model = Boom([])
    orchestrator = OrchestratorAgent(model, store, DocsReaderAgent(model, doc_file))
    try:
        orchestrator.handle("c1", "hello")
    except RuntimeError:
        pass
    assert [m.text for m in store.get("c1")] == ["hello"]


def test_empty_question_returns_structured_error(doc_file):
    model, _, orchestrator = build(doc_file, [tool_turn("ask_help_guide", question="  "), final_turn("ok")])
    orchestrator.handle("c1", "hello")
    tool_message = model.calls[1][1][-1]
    assert tool_message.tool_results[0].content["error"]["category"] == "invalid_input"
