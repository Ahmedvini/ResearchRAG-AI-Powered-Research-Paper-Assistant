using ResearchRag.Domain.Common;
using ResearchRag.Domain.Enums;

namespace ResearchRag.Domain.Entities;

public sealed class Document : Entity
{
    public Guid WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }
    public required string OriginalFileName { get; set; }
    public required string StoredFileName { get; set; }
    public required string StoragePath { get; set; }
    public long SizeBytes { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Queued;
    public string? FailureReason { get; set; }
    public string? Title { get; set; }
    public string? Authors { get; set; }
    public int? PublicationYear { get; set; }
    public string? Abstract { get; set; }
    public string? Keywords { get; set; }
    public List<DocumentChunk> Chunks { get; set; } = [];
    public PaperExtraction? PaperExtraction { get; set; }
}

