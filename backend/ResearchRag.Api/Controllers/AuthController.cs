using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchRag.Application.Abstractions;
using ResearchRag.Application.Auth;
using ResearchRag.Domain.Entities;
using ResearchRag.Infrastructure.Persistence;

namespace ResearchRag.Api.Controllers;

public sealed class AuthController(
    AppDbContext db,
    IAuthTokenService tokenService,
    IEmailSender emailSender,
    IConfiguration configuration,
    ILogger<AuthController> logger) : ApiControllerBase
{
    private readonly PasswordHasher<User> _hasher = new();

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var displayName = request.DisplayName.Trim();
        if (email.Length is < 5 or > 320 || !email.Contains('@')) return BadRequest("A valid email address is required.");
        if (string.IsNullOrWhiteSpace(displayName)) return BadRequest("Display name is required.");
        if (request.Password.Length < 8) return BadRequest("Password must be at least 8 characters.");

        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            return Conflict("Email is already registered.");
        }

        var user = new User
        {
            Email = email,
            DisplayName = displayName,
            PasswordHash = ""
        };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        var emailToken = tokenService.GenerateOpaqueToken();
        db.Users.Add(user);
        db.OneTimeTokens.Add(new OneTimeToken
        {
            User = user,
            TokenHash = tokenService.HashSecret(emailToken),
            Purpose = "email_verification",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(2)
        });

        var refresh = await AddRefreshTokenAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await SendTokenEmailAsync(
            user.Email,
            "Verify your ResearchRAG email",
            $"Welcome to ResearchRAG. Verify your email by opening: {FrontendLink("/verify-email", emailToken)}",
            cancellationToken);
        return tokenService.CreateAuthResponse(user, refresh);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.Include(x => x.RefreshTokens).SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid email or password.");
        }

        var refresh = await AddRefreshTokenAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return tokenService.CreateAuthResponse(user, refresh);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var hash = tokenService.HashSecret(request.RefreshToken);
        var existing = await db.RefreshTokens.Include(x => x.User).SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (existing?.User is null || !existing.IsActive)
        {
            return Unauthorized("Refresh token is invalid or expired.");
        }

        var replacement = tokenService.GenerateOpaqueToken();
        existing.RevokedAt = DateTimeOffset.UtcNow;
        existing.ReplacedByTokenHash = tokenService.HashSecret(replacement);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = existing.UserId,
            TokenHash = tokenService.HashSecret(replacement),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(14)
        });
        await db.SaveChangesAsync(cancellationToken);
        return tokenService.CreateAuthResponse(existing.User, replacement);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        var hash = tokenService.HashSecret(request.RefreshToken);
        var token = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash && x.UserId == CurrentUserId, cancellationToken);
        if (token is not null)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is not null)
        {
            var resetToken = tokenService.GenerateOpaqueToken();
            db.OneTimeTokens.Add(new OneTimeToken
            {
                UserId = user.Id,
                TokenHash = tokenService.HashSecret(resetToken),
                Purpose = "password_reset",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
            });
            await db.SaveChangesAsync(cancellationToken);
            await SendTokenEmailAsync(
                user.Email,
                "Reset your ResearchRAG password",
                $"Reset your password by opening: {FrontendLink("/reset-password", resetToken)}\nThis link expires in one hour.",
                cancellationToken);
        }
        return Accepted();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var hash = tokenService.HashSecret(request.Token);
        var token = await db.OneTimeTokens.Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == hash && x.Purpose == "password_reset", cancellationToken);
        if (token?.User is null || token.UsedAt is not null || token.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return BadRequest("Reset token is invalid or expired.");
        }

        token.User.PasswordHash = _hasher.HashPassword(token.User, request.NewPassword);
        token.UsedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var hash = tokenService.HashSecret(request.Token);
        var token = await db.OneTimeTokens.Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == hash && x.Purpose == "email_verification", cancellationToken);
        if (token?.User is null || token.UsedAt is not null || token.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return BadRequest("Verification token is invalid or expired.");
        }

        token.User.EmailVerified = true;
        token.UsedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Task<string> AddRefreshTokenAsync(User user, CancellationToken cancellationToken)
    {
        var refresh = tokenService.GenerateOpaqueToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            User = user,
            TokenHash = tokenService.HashSecret(refresh),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(14)
        });
        return Task.FromResult(refresh);
    }

    private string FrontendLink(string path, string token)
    {
        var baseUrl = (configuration["Email:FrontendBaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        return $"{baseUrl}{path}?token={Uri.EscapeDataString(token)}";
    }

    private async Task SendTokenEmailAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendAsync(to, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            // Email delivery must not fail the auth operation itself.
            logger.LogWarning(ex, "Failed to send '{Subject}' email to {To}.", subject, to);
        }
    }
}

