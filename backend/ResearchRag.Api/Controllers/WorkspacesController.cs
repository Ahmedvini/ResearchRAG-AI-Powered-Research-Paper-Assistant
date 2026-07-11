using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchRag.Application.Abstractions;
using ResearchRag.Application.Workspaces;
using ResearchRag.Domain.Entities;
using ResearchRag.Infrastructure.Persistence;

namespace ResearchRag.Api.Controllers;

[Authorize]
public sealed class WorkspacesController(AppDbContext db, IVectorStore vectorStore) : ApiControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<WorkspaceDto>> List(CancellationToken cancellationToken)
    {
        // Order before projecting: ordering by a DTO constructor member is not
        // translatable to SQL (it only worked on the in-memory test provider).
        return await db.Workspaces
            .Where(x => x.UserId == CurrentUserId)
            .OrderBy(x => x.Name)
            .Select(x => new WorkspaceDto(x.Id, x.Name, x.Description, x.Documents.Count, x.Chats.Count, x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<WorkspaceDto>> Create(UpsertWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (name.Length is 0 or > 200) return BadRequest("Workspace name must be 1-200 characters.");
        if (await db.Workspaces.AnyAsync(x => x.UserId == CurrentUserId && x.Name == name, cancellationToken))
        {
            return Conflict("A workspace with this name already exists.");
        }

        var workspace = new Workspace
        {
            UserId = CurrentUserId,
            Name = name,
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

        var name = request.Name.Trim();
        if (name.Length is 0 or > 200) return BadRequest("Workspace name must be 1-200 characters.");
        if (await db.Workspaces.AnyAsync(x => x.UserId == CurrentUserId && x.Name == name && x.Id != id, cancellationToken))
        {
            return Conflict("A workspace with this name already exists.");
        }

        workspace.Name = name;
        workspace.Description = request.Description.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var workspace = await db.Workspaces.SingleOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId, cancellationToken);
        if (workspace is null) return NotFound();

        var storagePaths = await db.Documents
            .Where(x => x.WorkspaceId == id)
            .Select(x => x.StoragePath)
            .ToListAsync(cancellationToken);

        db.Workspaces.Remove(workspace);
        await db.SaveChangesAsync(cancellationToken);

        await vectorStore.DeleteAsync(id, null, cancellationToken);
        foreach (var path in storagePaths)
        {
            DocumentsController.TryDeleteFile(path);
        }

        return NoContent();
    }
}

