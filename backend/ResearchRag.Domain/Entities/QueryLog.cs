using ResearchRag.Domain.Common;

namespace ResearchRag.Domain.Entities;

public sealed class QueryLog : Entity
{
    public Guid UserId { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Query { get; set; } = "";
    public int RetrievedChunks { get; set; }
    public int LatencyMs { get; set; }
}

