using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ResearchRag.Application.Abstractions;
using ResearchRag.Application.Auth;
using ResearchRag.Domain.Entities;

namespace ResearchRag.Infrastructure.Auth;

public sealed class AuthTokenService(IConfiguration configuration) : IAuthTokenService
{
    public string HashSecret(string secret)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(bytes);
    }

    public string GenerateOpaqueToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public AuthResponse CreateAuthResponse(User user, string refreshToken)
    {
        var issuer = configuration["Jwt:Issuer"] ?? "ResearchRAG";
        var audience = configuration["Jwt:Audience"] ?? "ResearchRAG";
        var signingKey = configuration["Jwt:SigningKey"] ?? "change-me-to-a-long-random-secret-of-at-least-32-bytes";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("display_name", user.DisplayName)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(issuer, audience, claims, expires: expiresAt.UtcDateTime, signingCredentials: credentials);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponse(
            jwt,
            refreshToken,
            expiresAt,
            new UserDto(user.Id, user.Email, user.DisplayName, user.Role.ToString(), user.EmailVerified));
    }
}
