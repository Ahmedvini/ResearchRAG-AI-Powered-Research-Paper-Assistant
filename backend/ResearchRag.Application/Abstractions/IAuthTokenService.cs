using ResearchRag.Application.Auth;
using ResearchRag.Domain.Entities;

namespace ResearchRag.Application.Abstractions;

public interface IAuthTokenService
{
    string HashSecret(string secret);
    string GenerateOpaqueToken();
    AuthResponse CreateAuthResponse(User user, string refreshToken);
}

