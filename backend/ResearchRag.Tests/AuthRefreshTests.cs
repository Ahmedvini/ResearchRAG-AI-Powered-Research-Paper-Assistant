using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ResearchRag.Api.Controllers;
using ResearchRag.Application.Abstractions;
using ResearchRag.Application.Auth;
using ResearchRag.Infrastructure.Auth;
using ResearchRag.Infrastructure.Persistence;
using Xunit;

namespace ResearchRag.Tests;

public sealed class AuthRefreshTests
{
    [Fact]
    public async Task Rotating_a_refresh_token_keeps_the_family()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);

        var registered = await controller.Register(new RegisterRequest("family@test.local", "Password123!", "Family Test"), CancellationToken.None);
        var initial = registered.Value!.RefreshToken;

        var rotated = await controller.Refresh(new RefreshRequest(initial), CancellationToken.None);
        Assert.NotNull(rotated.Value);

        var families = await db.RefreshTokens.Select(x => x.FamilyId).Distinct().ToListAsync();
        Assert.Single(families);
        Assert.NotEqual(Guid.Empty, families[0]);
    }

    [Fact]
    public async Task Reusing_a_rotated_refresh_token_revokes_the_whole_family()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);

        var registered = await controller.Register(new RegisterRequest("family@test.local", "Password123!", "Family Test"), CancellationToken.None);
        var initial = registered.Value!.RefreshToken;

        var rotated = await controller.Refresh(new RefreshRequest(initial), CancellationToken.None);
        var successor = rotated.Value!.RefreshToken;

        // Replaying the already-rotated token is the theft signal.
        var replay = await controller.Refresh(new RefreshRequest(initial), CancellationToken.None);
        Assert.IsType<UnauthorizedObjectResult>(replay.Result);

        // The successor was still unexpired, but the family revocation must
        // have killed it too.
        var afterReuse = await controller.Refresh(new RefreshRequest(successor), CancellationToken.None);
        Assert.IsType<UnauthorizedObjectResult>(afterReuse.Result);
        Assert.DoesNotContain(await db.RefreshTokens.ToListAsync(), x => x.RevokedAt == null);
    }

    [Fact]
    public async Task Refresh_tokens_from_separate_logins_are_not_revoked_together()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);

        var registered = await controller.Register(new RegisterRequest("family@test.local", "Password123!", "Family Test"), CancellationToken.None);
        var firstSession = registered.Value!.RefreshToken;
        var secondSession = (await controller.Login(new LoginRequest("family@test.local", "Password123!"), CancellationToken.None)).Value!.RefreshToken;

        // Burn the first session's token, then replay it to trigger family revocation.
        await controller.Refresh(new RefreshRequest(firstSession), CancellationToken.None);
        await controller.Refresh(new RefreshRequest(firstSession), CancellationToken.None);

        // The independent second session must survive.
        var stillValid = await controller.Refresh(new RefreshRequest(secondSession), CancellationToken.None);
        Assert.NotNull(stillValid.Value);
    }

    private static AuthController CreateController(AppDbContext db)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = "unit-test-signing-key-that-is-at-least-32-bytes!!"
        }).Build();
        return new AuthController(db, new AuthTokenService(configuration), new NoopEmailSender(), configuration, NullLogger<AuthController>.Instance);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class NoopEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
