using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchRag.Application.Dashboard;
using ResearchRag.Infrastructure.Persistence;

namespace ResearchRag.Api.Controllers;

[Authorize]
public sealed class DashboardController(AppDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<DashboardDto> Get(CancellationToken cancellationToken)
    {
        var docs = db.Documents.Where(x => x.Workspace!.UserId == CurrentUserId);
        var chats = db.ChatSessions.Where(x => x.Workspace!.UserId == CurrentUserId);
        var queries = db.QueryLogs.Where(x => x.UserId == CurrentUserId);

        var recent = await docs.OrderByDescending(x => x.CreatedAt).Take(5)
            .Select(x => new RecentDocumentDto(x.Id, x.OriginalFileName, x.Status.ToString(), x.CreatedAt))
            .ToListAsync(cancellationToken);

        // Order by the group key before projecting: ordering by a DTO
        // constructor member is not translatable to SQL.
        var papersPerYear = await docs.Where(x => x.PublicationYear != null)
            .GroupBy(x => x.PublicationYear!.Value)
            .OrderBy(x => x.Key)
            .Select(x => new YearCountDto(x.Key, x.Count()))
            .ToListAsync(cancellationToken);

        var topicCounts = await docs
            .Where(x => x.Keywords != null && x.Keywords != "")
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => x.Keywords!)
            .ToListAsync(cancellationToken);

        var topics = topicCounts
            .SelectMany(x => x.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(x => x.Count())
            .Take(8)
            .Select(x => new TopicCountDto(x.Key, x.Count()))
            .ToList();

        return new DashboardDto(
            await docs.CountAsync(cancellationToken),
            await chats.CountAsync(cancellationToken),
            await queries.CountAsync(cancellationToken),
            topics,
            recent,
            papersPerYear);
    }
}

