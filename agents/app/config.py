from pathlib import Path
from typing import Optional

from pydantic import SecretStr
from pydantic_settings import BaseSettings, SettingsConfigDict

REPO_ROOT = Path(__file__).resolve().parents[2]


class ConfigurationError(Exception):
    pass


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    gemini_api_key: Optional[SecretStr] = None
    gemini_model: str = ""
    inline_system_prompt: bool = False
    system_doc_path: Path = REPO_ROOT / "docs" / "SYSTEM.md"
    max_iterations: int = 25
    api_base_url: str = "http://localhost:5162"
    agent_api_key: Optional[SecretStr] = None
    api_timeout_seconds: float = 10.0

    def require_model_access(self) -> None:
        if self.gemini_api_key is None or not self.gemini_api_key.get_secret_value():
            raise ConfigurationError("GEMINI_API_KEY is not set")
        if not self.gemini_model:
            raise ConfigurationError("GEMINI_MODEL is not set")
