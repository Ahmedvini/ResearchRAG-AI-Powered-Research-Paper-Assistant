using ResearchRag.Application.Chats;

namespace ResearchRag.Application.Abstractions;

public interface IRagAnswerService
{
    Task<RagAnswerDto> AnswerAsync(Guid workspaceId, IReadOnlyList<Guid>? documentIds, string question, CancellationToken cancellationToken);
}

