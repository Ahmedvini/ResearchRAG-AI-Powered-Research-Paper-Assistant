using ResearchRag.Domain.Common;
using ResearchRag.Domain.Enums;

namespace ResearchRag.Domain.Entities;

public sealed class User : Entity
{
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string DisplayName { get; set; } = "";
    public bool EmailVerified { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public List<RefreshToken> RefreshTokens { get; set; } = [];
    public List<Workspace> Workspaces { get; set; } = [];
}

