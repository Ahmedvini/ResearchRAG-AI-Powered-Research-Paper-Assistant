using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using ResearchRag.Application.Abstractions;

namespace ResearchRag.Infrastructure.Ai;

public sealed class CohereRerankerProvider(HttpClient httpClient, IConfiguration configuration) : IRerankerProvider
{
    public async Task<IReadOnlyList<RetrievedChunk>> RerankAsync(string query, IReadOnlyList<RetrievedChunk> chunks, int topK, CancellationToken cancellationToken)
    {
        if (chunks.Count == 0) return [];
        var apiKey = configuration["Cohere:ApiKey"] ?? Environment.GetEnvironmentVariable("COHERE_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Cohere reranker requires Cohere:ApiKey or COHERE_API_KEY.");
        }

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var model = configuration["Cohere:RerankModel"] ?? "rerank-v3.5";
        var response = await httpClient.PostAsJsonAsync("/v2/rerank", new
        {
            model,
            query,
            documents = chunks.Select(x => x.Text).ToArray(),
            top_n = topK
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CohereRerankResponse>(cancellationToken: cancellationToken);

        return body?.Results
            .OrderBy(x => x.Index)
            .Select(result => chunks[result.Index] with { CombinedScore = result.RelevanceScore })
            .OrderByDescending(x => x.CombinedScore)
            .Take(topK)
            .ToList() ?? chunks.OrderByDescending(x => x.CombinedScore).Take(topK).ToList();
    }

    private sealed record CohereRerankResponse(IReadOnlyList<CohereRerankResult> Results);

    // Cohere returns snake_case; the default web JSON options only bridge
    // camelCase, so "relevance_score" needs an explicit property name or it
    // silently deserializes to 0 for every result.
    private sealed record CohereRerankResult(
        int Index,
        [property: JsonPropertyName("relevance_score")] double RelevanceScore);
}

