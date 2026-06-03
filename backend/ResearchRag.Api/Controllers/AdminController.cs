using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchRag.Infrastructure.Persistence;

namespace ResearchRag.Api.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminController(AppDbContext db) : ApiControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
    {
        var users = await db.Users
            .OrderBy(x => x.Email)
            .Select(x => new { x.Id, x.Email, x.DisplayName, Role = x.Role.ToString(), x.EmailVerified, x.CreatedAt })
            .ToListAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("query-logs")]
    public async Task<IActionResult> QueryLogs(CancellationToken cancellationToken)
    {
        var logs = await db.QueryLogs
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => new { x.Id, x.UserId, x.WorkspaceId, x.Query, x.RetrievedChunks, x.LatencyMs, x.CreatedAt })
            .ToListAsync(cancellationToken);
        return Ok(logs);
    }

    [HttpGet("processing-jobs")]
    public async Task<IActionResult> ProcessingJobs(CancellationToken cancellationToken)
    {
        var jobs = await db.ProcessingJobs
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => new { x.Id, x.DocumentId, Status = x.Status.ToString(), x.Attempts, x.LastError, x.StartedAt, x.CompletedAt })
            .ToListAsync(cancellationToken);
        return Ok(jobs);
    }

    [HttpGet("stats")]
    public async Task<object> Stats(CancellationToken cancellationToken)
    {
        return new
        {
            Users = await db.Users.CountAsync(cancellationToken),
            Workspaces = await db.Workspaces.CountAsync(cancellationToken),
            Documents = await db.Documents.CountAsync(cancellationToken),
            Chunks = await db.DocumentChunks.CountAsync(cancellationToken),
            Chats = await db.ChatSessions.CountAsync(cancellationToken),
            Queries = await db.QueryLogs.CountAsync(cancellationToken)
        };
    }
}
