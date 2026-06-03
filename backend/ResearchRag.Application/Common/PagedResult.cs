namespace ResearchRag.Application.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);

