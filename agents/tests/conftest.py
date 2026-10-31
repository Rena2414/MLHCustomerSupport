import pytest


@pytest.fixture
def doc_file(tmp_path):
    path = tmp_path / "SYSTEM.md"
    path.write_text("Frontend runs on port 4200. UNIQUE_DOC_MARKER_123", encoding="utf-8")
    return path
