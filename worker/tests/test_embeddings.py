from researchrag_worker.embeddings import HashEmbeddingProvider


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

