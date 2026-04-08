from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    app_name: str = "TukiFact AI"
    debug: bool = False

    # NATS
    nats_url: str = "nats://localhost:4222"

    # TukiFact API
    api_base_url: str = "http://localhost:5100"

    # AI Provider (for future LLM integration)
    ai_provider: str = "stub"  # "openai", "anthropic", "ollama", "stub"
    ai_api_key: str = ""
    ai_model: str = "gpt-4o-mini"

    class Config:
        env_prefix = "TUKIFACT_"
        env_file = ".env"


settings = Settings()
