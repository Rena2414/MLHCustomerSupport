from functools import lru_cache

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    gemini_api_key: str = ""
    backend_api_url: str = "http://localhost:5162/api"


@lru_cache
def get_settings() -> Settings:
    return Settings()
