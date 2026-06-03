namespace ResearchRag.Application.Chats;

public sealed record CreateChatRequest(Guid WorkspaceId, string Title);
public sealed record SendMessageRequest(string Question, IReadOnlyList<Guid>? DocumentIds);
public sealed record ChatDto(Guid Id, Guid WorkspaceId, string Title, DateTimeOffset CreatedAt);
public sealed record ChatMessageDto(Guid Id, string Role, string Content, IReadOnlyList<CitationDto> Citations, DateTimeOffset CreatedAt);
public sealed record CitationDto(Guid ChunkId, string DocumentName, string Section, int PageNumber, double RelevanceScore);
public sealed record RagAnswerDto(string Answer, IReadOnlyList<CitationDto> Citations);

