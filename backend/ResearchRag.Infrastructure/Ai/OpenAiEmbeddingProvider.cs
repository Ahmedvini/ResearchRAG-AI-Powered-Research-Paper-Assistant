using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using ResearchRag.Application.Abstractions;

namespace ResearchRag.Infrastructure.Ai;

public sealed class OpenAiEmbeddingProvider(HttpClient httpClient, IConfiguration configuration) : IEmbeddingProvider
{
    public async Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        var apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI embedding provider requires OpenAI:ApiKey or OPENAI_API_KEY.");
        }

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var model = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
        var response = await httpClient.PostAsJsonAsync("/v1/embeddings", new { model, input = text }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken);
        return body?.Data.FirstOrDefault()?.Embedding ?? [];
    }

    private sealed record EmbeddingResponse(IReadOnlyList<EmbeddingItem> Data);
    private sealed record EmbeddingItem(IReadOnlyList<float> Embedding);
}

