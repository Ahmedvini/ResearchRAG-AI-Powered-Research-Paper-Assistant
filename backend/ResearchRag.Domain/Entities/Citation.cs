using ResearchRag.Domain.Common;

namespace ResearchRag.Domain.Entities;

public sealed class Citation : Entity
{
    public Guid ChatMessageId { get; set; }
    public ChatMessage? ChatMessage { get; set; }
    public Guid ChunkId { get; set; }
    public string DocumentName { get; set; } = "";
    public string Section { get; set; } = "";
    public int PageNumber { get; set; }
    public double RelevanceScore { get; set; }
}

