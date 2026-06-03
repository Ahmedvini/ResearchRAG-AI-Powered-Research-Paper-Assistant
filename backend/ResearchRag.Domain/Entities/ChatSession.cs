using ResearchRag.Domain.Common;

namespace ResearchRag.Domain.Entities;

public sealed class ChatSession : Entity
{
    public Guid WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }
    public required string Title { get; set; }
    public List<ChatMessage> Messages { get; set; } = [];
}

