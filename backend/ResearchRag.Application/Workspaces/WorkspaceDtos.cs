namespace ResearchRag.Application.Workspaces;

public sealed record WorkspaceDto(Guid Id, string Name, string Description, int DocumentCount, int ChatCount, DateTimeOffset CreatedAt);
public sealed record UpsertWorkspaceRequest(string Name, string Description);

