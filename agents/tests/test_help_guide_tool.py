from app.core.messages import ToolCall
from app.core.tools import ToolRegistry
from app.tools.help_guide import build_read_help_guide_tool


def test_returns_document_content(doc_file):
    registry = ToolRegistry([build_read_help_guide_tool(doc_file)])
    result = registry.execute(ToolCall("1", "read_help_guide", {}))
    assert "UNIQUE_DOC_MARKER_123" in result.content["content"]


def test_missing_file_is_a_structured_error(tmp_path):
    registry = ToolRegistry([build_read_help_guide_tool(tmp_path / "missing.md")])
    result = registry.execute(ToolCall("1", "read_help_guide", {}))
    assert result.is_error
    assert result.content["error"]["category"] == "not_found"


def test_real_help_guide_is_well_formed_xml():
    import xml.etree.ElementTree as ElementTree

    from app.config import Settings

    root = ElementTree.parse(Settings().system_doc_path).getroot()
    entries = root.findall(".//qa")
    ids = [entry.get("id") for entry in entries]
    assert len(entries) >= 20
    assert len(ids) == len(set(ids))
    for entry in entries:
        assert (entry.findtext("question") or "").strip()
        assert "".join(entry.find("answer").itertext()).strip()
