namespace ResearchRag.Application.Abstractions;

public interface IRerankerProvider
{
    Task<IReadOnlyList<RetrievedChunk>> RerankAsync(string query, IReadOnlyList<RetrievedChunk> chunks, int topK, CancellationToken cancellationToken);
}

