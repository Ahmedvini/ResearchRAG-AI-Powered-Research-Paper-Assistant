using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using ResearchRag.Application.Abstractions;

namespace ResearchRag.Infrastructure.Ai;

public sealed class OllamaEmbeddingProvider(HttpClient httpClient, IConfiguration configuration) : IEmbeddingProvider
{
    public async Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        var model = configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
        var response = await httpClient.PostAsJsonAsync("/api/embeddings", new { model, prompt = text }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken: cancellationToken);
        return body?.Embedding ?? [];
    }

    private sealed record OllamaEmbeddingResponse(IReadOnlyList<float> Embedding);
}

