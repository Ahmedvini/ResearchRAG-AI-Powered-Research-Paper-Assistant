using Microsoft.Extensions.Logging;
using ResearchRag.Application.Abstractions;

namespace ResearchRag.Infrastructure.Email;

/// <summary>
/// Development fallback used when no SMTP host is configured: writes the email
/// (including verification/reset links) to the application log instead of
/// silently dropping it.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        logger.LogInformation("Email (no SMTP configured) to {To}: {Subject}\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
