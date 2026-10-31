import re
import subprocess
from pathlib import Path

from app.config import REPO_ROOT

KEY_PATTERNS = [re.compile(r"AIza[0-9A-Za-z_\-]{35}"), re.compile(r"(?i)gemini_api_key\s*[=:]\s*['\"]?[A-Za-z0-9_\-]{20,}")]


def tracked_and_new_files():
    output = subprocess.run(
        ["git", "ls-files", "--cached", "--others", "--exclude-standard"],
        cwd=REPO_ROOT, capture_output=True, text=True, check=True,
    ).stdout.split("\n")
    return [REPO_ROOT / name for name in output if name]


def test_no_api_keys_in_repository_files():
    offenders = []
    for path in tracked_and_new_files():
        if not path.is_file() or path.stat().st_size > 2_000_000 or path.name == "test_no_secrets_committed.py":
            continue
        try:
            text = path.read_text(encoding="utf-8")
        except (UnicodeDecodeError, OSError):
            continue
        if any(pattern.search(text) for pattern in KEY_PATTERNS):
            offenders.append(str(path.relative_to(REPO_ROOT)))
    assert offenders == []


def test_env_files_are_ignored():
    result = subprocess.run(["git", "check-ignore", "agents/.env"], cwd=REPO_ROOT, capture_output=True, text=True)
    assert result.returncode == 0
