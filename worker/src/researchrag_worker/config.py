from pydantic import Field
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    database_url: str = Field(default="mysql+pymysql://researchrag:researchrag@localhost:3306/researchrag", alias="DATABASE_URL")
    qdrant_url: str = Field(default="http://localhost:6333", alias="QDRANT_URL")
    qdrant_collection: str = Field(default="researchrag_chunks", alias="QDRANT_COLLECTION")
    upload_root: str = Field(default="/app/uploads", alias="UPLOAD_ROOT")
    embedding_model: str = Field(default="BAAI/bge-large-en-v1.5", alias="EMBEDDING_MODEL")
    poll_seconds: int = Field(default=5, alias="POLL_SECONDS")
    chunk_size: int = Field(default=1200, alias="CHUNK_SIZE")
    chunk_overlap: int = Field(default=180, alias="CHUNK_OVERLAP")

