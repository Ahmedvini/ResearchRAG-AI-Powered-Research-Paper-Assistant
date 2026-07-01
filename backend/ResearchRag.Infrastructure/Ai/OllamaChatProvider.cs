using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using ResearchRag.Application.Abstractions;

namespace ResearchRag.Infrastructure.Ai;

public sealed class OllamaChatProvider(HttpClient httpClient, IConfiguration configuration) : ILLMProvider
{
    public async Task<string> GenerateAnswerAsync(string question, IReadOnlyList<RetrievedChunk> chunks, CancellationToken cancellationToken)
    {
        var model = configuration["Ollama:ChatModel"] ?? "llama3.1";
        var evidence = string.Join("\n\n", chunks.Select((chunk, index) =>
            $"Source {index + 1}: {chunk.DocumentName}, {chunk.Section}, page {chunk.PageNumber}\n{chunk.Text}"));

        var request = new
        {
            model,
            stream = false,
            prompt = $"Answer from the evidence only.\n\nQuestion: {question}\n\nEvidence:\n{evidence}"
        };

        var response = await httpClient.PostAsJsonAsync("/api/generate", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: cancellationToken);
        return body?.Response?.Trim() ?? "No answer was returned by Ollama.";
    }

    private sealed record OllamaGenerateResponse(string? Response);
}

