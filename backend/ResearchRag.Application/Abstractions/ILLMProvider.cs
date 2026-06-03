namespace ResearchRag.Application.Abstractions;

public interface ILLMProvider
{
    Task<string> GenerateAnswerAsync(string question, IReadOnlyList<RetrievedChunk> chunks, CancellationToken cancellationToken);
}

