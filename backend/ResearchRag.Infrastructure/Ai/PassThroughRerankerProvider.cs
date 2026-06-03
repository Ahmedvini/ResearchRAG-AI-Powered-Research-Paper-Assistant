using ResearchRag.Application.Abstractions;

namespace ResearchRag.Infrastructure.Ai;

public sealed class PassThroughRerankerProvider : IRerankerProvider
{
    public Task<IReadOnlyList<RetrievedChunk>> RerankAsync(string query, IReadOnlyList<RetrievedChunk> chunks, int topK, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<RetrievedChunk>>(chunks.OrderByDescending(x => x.CombinedScore).Take(topK).ToList());
    }
}

