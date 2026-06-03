namespace ResearchRag.Application.Abstractions;

public interface IEmbeddingProvider
{
    Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken);
}

