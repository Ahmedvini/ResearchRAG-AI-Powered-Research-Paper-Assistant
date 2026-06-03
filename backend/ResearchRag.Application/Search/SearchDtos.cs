namespace ResearchRag.Application.Search;

public sealed record SearchResultDto(string Type, Guid Id, string Title, string Snippet, double Score);

