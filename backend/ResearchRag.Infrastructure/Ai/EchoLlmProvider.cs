using ResearchRag.Application.Abstractions;

namespace ResearchRag.Infrastructure.Ai;

public sealed class EchoLlmProvider : ILLMProvider
{
    public Task<string> GenerateAnswerAsync(string question, IReadOnlyList<RetrievedChunk> chunks, CancellationToken cancellationToken)
    {
        if (chunks.Count == 0)
        {
            return Task.FromResult("I could not find enough cited evidence in the selected workspace documents to answer that question.");
        }

        var evidence = string.Join("\n", chunks.Take(3).Select((chunk, index) => $"{index + 1}. {Trim(chunk.Text, 420)}"));
        return Task.FromResult($"Based on the retrieved paper excerpts, the answer to \"{question}\" is grounded in these sources:\n\n{evidence}");
    }

    private static string Trim(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}

