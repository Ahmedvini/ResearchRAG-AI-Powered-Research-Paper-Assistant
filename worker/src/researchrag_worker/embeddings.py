import hashlib
import math
import json
from abc import ABC, abstractmethod
from urllib import request


class EmbeddingProvider(ABC):
    dimension = 384

    @abstractmethod
    def embed(self, text: str) -> list[float]:
        raise NotImplementedError


def hash_bucket(token: str, dimension: int) -> int:
    """Deterministic token bucket shared with the backend's HashEmbeddingProvider.

    Uses the first 4 bytes of MD5 (big-endian) so that queries embedded by the
    C# API land in the same vector space as chunks embedded here. Python's
    builtin hash() is seed-randomized per process and must not be used.
    """
    digest = hashlib.md5(token.encode("utf-8")).digest()
    return int.from_bytes(digest[:4], "big") % dimension


class HashEmbeddingProvider(EmbeddingProvider):
    dimension = 384

    def embed(self, text: str) -> list[float]:
        vector = [0.0] * self.dimension
        for token in text.lower().split():
            vector[hash_bucket(token, self.dimension)] += 1.0
        magnitude = math.sqrt(sum(value * value for value in vector))
        if magnitude:
            vector = [value / magnitude for value in vector]
        return vector


class OpenAiEmbeddingProvider(EmbeddingProvider):
    dimension = 1536

    def __init__(self, api_key: str, model: str = "text-embedding-3-small", base_url: str = "https://api.openai.com") -> None:
        self.api_key = api_key
        self.model = model
        self.base_url = base_url.rstrip("/")

    def embed(self, text: str) -> list[float]:
        payload = json.dumps({"model": self.model, "input": text}).encode("utf-8")
        http_request = request.Request(
            f"{self.base_url}/v1/embeddings",
            data=payload,
            headers={"Authorization": f"Bearer {self.api_key}", "Content-Type": "application/json"},
            method="POST",
        )
        with request.urlopen(http_request, timeout=60) as response:
            body = json.loads(response.read().decode("utf-8"))
        embedding = body["data"][0]["embedding"]
        self.dimension = len(embedding)
        return embedding


class OllamaEmbeddingProvider(EmbeddingProvider):
    dimension = 768

    def __init__(self, model: str = "nomic-embed-text", base_url: str = "http://localhost:11434") -> None:
        self.model = model
        self.base_url = base_url.rstrip("/")

    def embed(self, text: str) -> list[float]:
        payload = json.dumps({"model": self.model, "prompt": text}).encode("utf-8")
        http_request = request.Request(
            f"{self.base_url}/api/embeddings",
            data=payload,
            headers={"Content-Type": "application/json"},
            method="POST",
        )
        with request.urlopen(http_request, timeout=60) as response:
            body = json.loads(response.read().decode("utf-8"))
        embedding = body["embedding"]
        self.dimension = len(embedding)
        return embedding


def create_embedding_provider(provider: str, model: str, openai_api_key: str | None, openai_base_url: str, ollama_base_url: str) -> EmbeddingProvider:
    if provider.lower() == "openai":
        if not openai_api_key:
            raise ValueError("OPENAI_API_KEY is required when EMBEDDING_PROVIDER=openai")
        return OpenAiEmbeddingProvider(openai_api_key, model or "text-embedding-3-small", openai_base_url)
    if provider.lower() == "ollama":
        return OllamaEmbeddingProvider(model or "nomic-embed-text", ollama_base_url)
    return HashEmbeddingProvider()
