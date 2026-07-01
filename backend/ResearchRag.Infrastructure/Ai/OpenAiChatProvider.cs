using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using ResearchRag.Application.Abstractions;

namespace ResearchRag.Infrastructure.Ai;

public sealed class OpenAiChatProvider(HttpClient httpClient, IConfiguration configuration) : ILLMProvider
{
    public async Task<string> GenerateAnswerAsync(string question, IReadOnlyList<RetrievedChunk> chunks, CancellationToken cancellationToken)
    {
        var apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI chat provider requires OpenAI:ApiKey or OPENAI_API_KEY.");
        }

        var model = configuration["OpenAI:ChatModel"] ?? "gpt-4.1-mini";
        var evidence = string.Join("\n\n", chunks.Select((chunk, index) =>
            $"Source {index + 1}: {chunk.DocumentName}, {chunk.Section}, page {chunk.PageNumber}\n{chunk.Text}"));

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var request = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = "You are ResearchRAG. Answer only from the supplied evidence. If evidence is weak, say so. Keep citations implicit because the API attaches structured citations separately." },
                new { role = "user", content = $"Question: {question}\n\nEvidence:\n{evidence}" }
            },
            temperature = 0.2
        };

        var response = await httpClient.PostAsJsonAsync("/v1/chat/completions", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(cancellationToken: cancellationToken);
        return body?.Choices.FirstOrDefault()?.Message.Content?.Trim() ?? "No answer was returned by the configured chat model.";
    }

    private sealed record OpenAiChatResponse(IReadOnlyList<OpenAiChoice> Choices);
    private sealed record OpenAiChoice(OpenAiMessage Message);
    private sealed record OpenAiMessage(string Content);
}

