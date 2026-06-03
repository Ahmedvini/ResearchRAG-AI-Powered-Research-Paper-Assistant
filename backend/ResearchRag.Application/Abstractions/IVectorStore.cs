namespace ResearchRag.Application.Abstractions;

public interface IVectorStore
{
    Task<IReadOnlyList<VectorSearchHit>> SearchAsync(Guid workspaceId, IReadOnlyList<Guid>? documentIds, IReadOnlyList<float> vector, int topK, CancellationToken cancellationToken);
}

