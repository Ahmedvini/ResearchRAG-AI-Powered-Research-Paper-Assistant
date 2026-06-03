using ResearchRag.Application.Abstractions;
using ResearchRag.Application.Services;
using Xunit;

namespace ResearchRag.Tests;

public sealed class HybridScoreMergerTests
{
    [Fact]
    public void Merge_combines_keyword_and_vector_scores_for_same_chunk()
    {
        var chunkId = Guid.NewGuid();
        var keyword = new RetrievedChunk(chunkId, Guid.NewGuid(), "paper.pdf", "keyword", "Methods", 2, 0, 0.8, 0.8);
        var vector = keyword with { SemanticScore = 0.6, KeywordScore = 0, CombinedScore = 0.6 };

        var result = HybridScoreMerger.Merge([keyword], [vector]).Single();

        Assert.Equal(chunkId, result.ChunkId);
        Assert.True(result.CombinedScore > 0.65);
        Assert.Equal(0.8, result.KeywordScore);
        Assert.Equal(0.6, result.SemanticScore);
    }

    [Fact]
    public void Merge_orders_by_combined_score()
    {
        var low = new RetrievedChunk(Guid.NewGuid(), Guid.NewGuid(), "a.pdf", "low", "Intro", 1, 0.1, 0, 0.1);
        var high = new RetrievedChunk(Guid.NewGuid(), Guid.NewGuid(), "b.pdf", "high", "Results", 3, 0.9, 0, 0.9);

        var result = HybridScoreMerger.Merge([], [low, high]);

        Assert.Equal(high.ChunkId, result[0].ChunkId);
    }
}

