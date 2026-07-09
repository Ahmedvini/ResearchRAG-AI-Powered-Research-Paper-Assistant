using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using ResearchRag.Application.Abstractions;

namespace ResearchRag.Infrastructure.Email;

public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        var host = configuration["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host is not configured.");
        var port = int.TryParse(configuration["Smtp:Port"], out var parsed) ? parsed : 587;
        var enableSsl = !string.Equals(configuration["Smtp:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase);
        var from = configuration["Email:From"] ?? "no-reply@researchrag.local";

        using var client = new SmtpClient(host, port) { EnableSsl = enableSsl };
        var username = configuration["Smtp:Username"];
        if (!string.IsNullOrEmpty(username))
        {
            client.Credentials = new NetworkCredential(username, configuration["Smtp:Password"]);
        }

        using var message = new MailMessage(from, to, subject, body);
        await client.SendMailAsync(message, cancellationToken);
    }
}
