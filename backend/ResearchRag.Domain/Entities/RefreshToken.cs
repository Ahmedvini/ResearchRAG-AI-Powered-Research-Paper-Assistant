using ResearchRag.Domain.Common;

namespace ResearchRag.Domain.Entities;

public sealed class RefreshToken : Entity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public required string TokenHash { get; set; }
    /// <summary>
    /// Groups a login's rotation chain. A new family starts at login/register;
    /// rotation keeps the family. Presenting an already-rotated token revokes
    /// the whole family (reuse implies the token leaked).
    /// </summary>
    public Guid FamilyId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public bool IsActive => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;
}

