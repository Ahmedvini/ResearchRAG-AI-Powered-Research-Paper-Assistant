using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchRag.Application.Search;
using ResearchRag.Infrastructure.Persistence;

namespace ResearchRag.Api.Controllers;

[Authorize]
public sealed class SearchController(AppDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<SearchResultDto>> Search([FromQuery] string q, [FromQuery] int? year, [FromQuery] string? author, CancellationToken cancellationToken)
    {
        q = q.Trim();
        if (q.Length < 2) return [];

        var documents = db.Documents.Where(x => x.Workspace!.UserId == CurrentUserId);
        if (year is not null) documents = documents.Where(x => x.PublicationYear == year);
        if (!string.IsNullOrWhiteSpace(author)) documents = documents.Where(x => x.Authors != null && x.Authors.Contains(author));

        var docResults = await documents
            .Where(x => x.OriginalFileName.Contains(q) || (x.Title != null && x.Title.Contains(q)) || (x.Abstract != null && x.Abstract.Contains(q)))
            .Take(20)
            .Select(x => new SearchResultDto("paper", x.Id, x.Title ?? x.OriginalFileName, x.Abstract ?? x.OriginalFileName, 1))
            .ToListAsync(cancellationToken);

        var workspaceResults = await db.Workspaces
            .Where(x => x.UserId == CurrentUserId && (x.Name.Contains(q) || x.Description.Contains(q)))
            .Take(10)
            .Select(x => new SearchResultDto("workspace", x.Id, x.Name, x.Description, 0.8))
            .ToListAsync(cancellationToken);

        return docResults.Concat(workspaceResults).OrderByDescending(x => x.Score).ToList();
    }
}

