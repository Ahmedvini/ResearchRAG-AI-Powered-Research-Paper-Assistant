namespace ResearchRag.Application.Abstractions;

public interface IDocumentProcessorClient
{
    Task EnqueueAsync(Guid documentId, CancellationToken cancellationToken);
}

