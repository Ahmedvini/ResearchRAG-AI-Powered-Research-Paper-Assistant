using ResearchRag.Application.Abstractions;

namespace ResearchRag.Application.Services;

public static class HybridScoreMerger
{
    public static IReadOnlyList<RetrievedChunk> Merge(
        IEnumerable<RetrievedChunk> keywordHits,
        IEnumerable<RetrievedChunk> vectorHits,
        double keywordWeight = 0.35,
        double vectorWeight = 0.65)
    {
        var merged = new Dictionary<Guid, RetrievedChunk>();

        foreach (var hit in keywordHits)
        {
            merged[hit.ChunkId] = hit with { CombinedScore = hit.KeywordScore * keywordWeight };
        }

        foreach (var hit in vectorHits)
        {
            if (merged.TryGetValue(hit.ChunkId, out var existing))
            {
                merged[hit.ChunkId] = existing with
                {
                    SemanticScore = hit.SemanticScore,
                    CombinedScore = existing.KeywordScore * keywordWeight + hit.SemanticScore * vectorWeight
                };
            }
            else
            {
                merged[hit.ChunkId] = hit with { CombinedScore = hit.SemanticScore * vectorWeight };
            }
        }

        return merged.Values.OrderByDescending(x => x.CombinedScore).ToList();
    }
}

