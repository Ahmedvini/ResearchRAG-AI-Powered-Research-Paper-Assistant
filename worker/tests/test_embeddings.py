import pytest

from researchrag_worker.embeddings import HashEmbeddingProvider, OllamaEmbeddingProvider, create_embedding_provider


def test_hash_embedding_is_normalized_and_stable_length():
    provider = HashEmbeddingProvider()
    vector = provider.embed("retrieval augmented generation retrieval")

    assert len(vector) == provider.dimension
    assert 0.99 < sum(value * value for value in vector) <= 1.01


def test_hash_embedding_handles_empty_text():
    provider = HashEmbeddingProvider()
    vector = provider.embed("")

    assert len(vector) == provider.dimension
    assert sum(vector) == 0


def test_embedding_factory_defaults_to_hash_provider():
    provider = create_embedding_provider("hash", "ignored", None, "https://api.openai.com", "http://localhost:11434")

    assert isinstance(provider, HashEmbeddingProvider)


def test_embedding_factory_requires_openai_key():
    with pytest.raises(ValueError):
        create_embedding_provider("openai", "text-embedding-3-small", None, "https://api.openai.com", "http://localhost:11434")


def test_embedding_factory_supports_ollama_provider():
    provider = create_embedding_provider("ollama", "nomic-embed-text", None, "https://api.openai.com", "http://localhost:11434")

    assert isinstance(provider, OllamaEmbeddingProvider)
