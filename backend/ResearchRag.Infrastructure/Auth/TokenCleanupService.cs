using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResearchRag.Infrastructure.Persistence;

namespace ResearchRag.Infrastructure.Auth;

/// <summary>
/// Periodically deletes refresh tokens and one-time tokens that can no longer
/// be used, so the tables do not grow without bound.
/// </summary>
public sealed class TokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<TokenCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Token cleanup pass failed; will retry next interval.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        if (db.Database.IsRelational())
        {
            var refresh = await db.RefreshTokens
                .Where(x => x.ExpiresAt < now || (x.RevokedAt != null && x.RevokedAt < now.AddDays(-7)))
                .ExecuteDeleteAsync(cancellationToken);
            var oneTime = await db.OneTimeTokens
                .Where(x => x.ExpiresAt < now || x.UsedAt != null)
                .ExecuteDeleteAsync(cancellationToken);
            if (refresh + oneTime > 0)
            {
                logger.LogInformation("Token cleanup removed {Refresh} refresh tokens and {OneTime} one-time tokens.", refresh, oneTime);
            }
            return;
        }

        // The in-memory provider does not support ExecuteDelete.
        db.RefreshTokens.RemoveRange(db.RefreshTokens.Where(x => x.ExpiresAt < now || (x.RevokedAt != null && x.RevokedAt < now.AddDays(-7))));
        db.OneTimeTokens.RemoveRange(db.OneTimeTokens.Where(x => x.ExpiresAt < now || x.UsedAt != null));
        await db.SaveChangesAsync(cancellationToken);
    }
}
