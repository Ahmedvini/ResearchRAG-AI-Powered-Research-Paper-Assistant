using ResearchRag.Domain.Common;

namespace ResearchRag.Domain.Entities;

public sealed class Workspace : Entity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public List<Document> Documents { get; set; } = [];
    public List<ChatSession> Chats { get; set; } = [];
}

