using ResearchRag.Domain.Common;

namespace ResearchRag.Domain.Entities;

public sealed class DocumentChunk : Entity
{
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
    public Guid WorkspaceId { get; set; }
    public required string Text { get; set; }
    public int PageNumber { get; set; }
    public string SectionName { get; set; } = "Unknown";
    public string VectorId { get; set; } = "";
}

