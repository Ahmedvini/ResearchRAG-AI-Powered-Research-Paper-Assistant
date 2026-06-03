using ResearchRag.Domain.Common;

namespace ResearchRag.Domain.Entities;

public sealed class OneTimeToken : Entity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public required string TokenHash { get; set; }
    public required string Purpose { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
}

