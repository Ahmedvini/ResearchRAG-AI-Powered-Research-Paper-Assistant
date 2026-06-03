using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchRag.Application.Workspaces;
using ResearchRag.Domain.Entities;
using ResearchRag.Infrastructure.Persistence;

namespace ResearchRag.Api.Controllers;

[Authorize]
public sealed class WorkspacesController(AppDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<WorkspaceDto>> List(CancellationToken cancellationToken)
    {
        return await db.Workspaces
            .Where(x => x.UserId == CurrentUserId)
            .Select(x => new WorkspaceDto(x.Id, x.Name, x.Description, x.Documents.Count, x.Chats.Count, x.CreatedAt))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<WorkspaceDto>> Create(UpsertWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var workspace = new Workspace
        {
            UserId = CurrentUserId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim()
        };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = workspace.Id }, new WorkspaceDto(workspace.Id, workspace.Name, workspace.Description, 0, 0, workspace.CreatedAt));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkspaceDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var workspace = await db.Workspaces
            .Where(x => x.UserId == CurrentUserId && x.Id == id)
            .Select(x => new WorkspaceDto(x.Id, x.Name, x.Description, x.Documents.Count, x.Chats.Count, x.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
        return workspace is null ? NotFound() : workspace;
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpsertWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var workspace = await db.Workspaces.SingleOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId, cancellationToken);
        if (workspace is null) return NotFound();
        workspace.Name = request.Name.Trim();
        workspace.Description = request.Description.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var workspace = await db.Workspaces.SingleOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId, cancellationToken);
        if (workspace is null) return NotFound();
        db.Workspaces.Remove(workspace);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

