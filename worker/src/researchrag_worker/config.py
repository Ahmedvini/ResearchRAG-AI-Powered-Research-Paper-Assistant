from pydantic import Field
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    database_url: str = Field(default="mysql+pymysql://researchrag:researchrag@localhost:3306/researchrag", alias="DATABASE_URL")
    qdrant_url: str = Field(default="http://localhost:6333", alias="QDRANT_URL")
    qdrant_collection: str = Field(default="researchrag_chunks", alias="QDRANT_COLLECTION")
    upload_root: str = Field(default="/app/uploads", alias="UPLOAD_ROOT")
    embedding_provider: str = Field(default="hash", alias="EMBEDDING_PROVIDER")
    # Chat provider powers LLM-backed paper extraction; "echo" (the default)
    # falls back to regex heuristics.
    chat_provider: str = Field(default="echo", alias="CHAT_PROVIDER")
    openai_chat_model: str = Field(default="gpt-4.1-mini", alias="OPENAI_CHAT_MODEL")
    ollama_chat_model: str = Field(default="llama3.1", alias="OLLAMA_CHAT_MODEL")
    # Explicit override; when empty, the provider-specific model below is used so
    # the worker stays in sync with the backend's OpenAI/Ollama defaults.
    embedding_model: str = Field(default="", alias="EMBEDDING_MODEL")
    openai_embedding_model: str = Field(default="text-embedding-3-small", alias="OPENAI_EMBEDDING_MODEL")
    ollama_embedding_model: str = Field(default="nomic-embed-text", alias="OLLAMA_EMBEDDING_MODEL")
    openai_api_key: str | None = Field(default=None, alias="OPENAI_API_KEY")
    openai_base_url: str = Field(default="https://api.openai.com", alias="OPENAI_BASE_URL")
    ollama_base_url: str = Field(default="http://localhost:11434", alias="OLLAMA_BASE_URL")
    poll_seconds: int = Field(default=5, alias="POLL_SECONDS")
    chunk_size: int = Field(default=1200, alias="CHUNK_SIZE")
    chunk_overlap: int = Field(default=180, alias="CHUNK_OVERLAP")
    max_attempts: int = Field(default=3, alias="MAX_ATTEMPTS")
    stale_running_minutes: int = Field(default=15, alias="STALE_RUNNING_MINUTES")

    def resolve_embedding_model(self) -> str:
        if self.embedding_model:
            return self.embedding_model
        if self.embedding_provider.lower() == "openai":
            return self.openai_embedding_model
        return self.ollama_embedding_model
