using ResearchRag.Infrastructure.Ai;
using Xunit;

namespace ResearchRag.Tests;

public sealed class HashEmbeddingProviderTests
{
    [Fact]
    public async Task Embed_matches_worker_golden_buckets()
    {
        // Golden values shared with worker/tests/test_embeddings.py.
        // If either side changes its hashing, both tests must change together.
        var provider = new HashEmbeddingProvider();
        var vector = await provider.EmbedAsync("Retrieval Augmented Generation", CancellationToken.None);

        var nonzero = vector
            .Select((value, index) => (value, index))
            .Where(x => x.value > 0)
            .Select(x => x.index)
            .OrderBy(x => x)
            .ToArray();

        Assert.Equal([12, 266, 347], nonzero);
    }

    [Fact]
    public async Task Embed_is_normalized()
    {
        var provider = new HashEmbeddingProvider();
        var vector = await provider.EmbedAsync("retrieval augmented generation retrieval", CancellationToken.None);

        Assert.Equal(384, vector.Count);
        var magnitude = vector.Sum(x => (double)x * x);
        Assert.InRange(magnitude, 0.99, 1.01);
    }

    [Fact]
    public async Task Embed_is_case_insensitive()
    {
        var provider = new HashEmbeddingProvider();
        var upper = await provider.EmbedAsync("EEG Motor Imagery", CancellationToken.None);
        var lower = await provider.EmbedAsync("eeg motor imagery", CancellationToken.None);

        Assert.Equal(upper, lower);
    }
}
