using ResearchRag.Domain.Common;

namespace ResearchRag.Domain.Entities;

public sealed class ChatMessage : Entity
{
    public Guid ChatSessionId { get; set; }
    public ChatSession? ChatSession { get; set; }
    public required string Role { get; set; }
    public required string Content { get; set; }
    public List<Citation> Citations { get; set; } = [];
}

