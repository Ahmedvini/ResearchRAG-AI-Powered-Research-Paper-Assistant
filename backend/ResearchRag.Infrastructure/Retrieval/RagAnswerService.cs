using ResearchRag.Application.Abstractions;
using ResearchRag.Application.Chats;
using ResearchRag.Application.Services;

namespace ResearchRag.Infrastructure.Retrieval;

public sealed class RagAnswerService(IRetrievalService retrievalService, ILLMProvider llmProvider) : IRagAnswerService
{
    public async Task<RagAnswerDto> AnswerAsync(Guid workspaceId, IReadOnlyList<Guid>? documentIds, string question, CancellationToken cancellationToken)
    {
        var chunks = await retrievalService.RetrieveAsync(workspaceId, documentIds, question, topK: 8, cancellationToken);
        var answer = await llmProvider.GenerateAnswerAsync(question, chunks, cancellationToken);
        var citations = chunks.Select(CitationFactory.FromChunk).ToList();
        return new RagAnswerDto(answer, citations);
    }
}

