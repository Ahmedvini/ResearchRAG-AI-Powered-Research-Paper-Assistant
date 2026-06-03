using ResearchRag.Domain.Enums;

namespace ResearchRag.Application.Documents;

public sealed record DocumentDto(
    Guid Id,
    Guid WorkspaceId,
    string OriginalFileName,
    DocumentStatus Status,
    string? Title,
    string? Authors,
    int? PublicationYear,
    string? Abstract,
    string? Keywords,
    DateTimeOffset CreatedAt);

public sealed record DocumentChunkDto(Guid Id, string Text, int PageNumber, string SectionName);

