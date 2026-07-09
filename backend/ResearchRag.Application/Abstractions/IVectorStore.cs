namespace ResearchRag.Application.Abstractions;

public interface IVectorStore
{
    Task<IReadOnlyList<VectorSearchHit>> SearchAsync(Guid workspaceId, IReadOnlyList<Guid>? documentIds, IReadOnlyList<float> vector, int topK, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes all points for the workspace, or only those of a single document
    /// when <paramref name="documentId"/> is provided. Best-effort: implementations
    /// should not throw when the vector store is unavailable.
    /// </summary>
    Task DeleteAsync(Guid workspaceId, Guid? documentId, CancellationToken cancellationToken);
}
