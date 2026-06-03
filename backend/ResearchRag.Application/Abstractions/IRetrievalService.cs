namespace ResearchRag.Application.Abstractions;

public interface IRetrievalService
{
    Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(Guid workspaceId, IReadOnlyList<Guid>? documentIds, string query, int topK, CancellationToken cancellationToken);
}

