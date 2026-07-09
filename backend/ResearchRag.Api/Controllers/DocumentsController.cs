using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchRag.Application.Abstractions;
using ResearchRag.Application.Documents;
using ResearchRag.Domain.Entities;
using ResearchRag.Domain.Enums;
using ResearchRag.Infrastructure.Persistence;

namespace ResearchRag.Api.Controllers;

[Authorize]
public sealed class DocumentsController(AppDbContext db, IConfiguration configuration, IDocumentProcessorClient processor, IVectorStore vectorStore) : ApiControllerBase
{
    [HttpGet("workspace/{workspaceId:guid}")]
    public async Task<IReadOnlyList<DocumentDto>> List(Guid workspaceId, CancellationToken cancellationToken)
    {
        return await db.Documents
            .Where(x => x.WorkspaceId == workspaceId && x.Workspace!.UserId == CurrentUserId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    [HttpPost("workspace/{workspaceId:guid}/upload")]
    [RequestSizeLimit(60_000_000)]
    public async Task<ActionResult<DocumentDto>> Upload(Guid workspaceId, IFormFile file, CancellationToken cancellationToken)
    {
        var ownsWorkspace = await db.Workspaces.AnyAsync(x => x.Id == workspaceId && x.UserId == CurrentUserId, cancellationToken);
        if (!ownsWorkspace) return NotFound("Workspace not found.");
        if (file.Length == 0) return BadRequest("PDF file is empty.");
        if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)) return BadRequest("Only PDF files are supported.");
        if (file.ContentType != "application/pdf") return BadRequest("Invalid PDF content type.");
        var maxBytes = configuration.GetValue<long>("Storage:MaxPdfBytes", 52_428_800);
        if (file.Length > maxBytes) return BadRequest($"PDF exceeds the {maxBytes} byte limit.");

        var root = configuration["Storage:UploadRoot"] ?? "uploads";
        Directory.CreateDirectory(root);
        var stored = $"{Guid.NewGuid():N}.pdf";
        // Store an absolute path: the worker resolves StoragePath from its own
        // working directory, so a relative path only works by accident.
        var path = Path.GetFullPath(Path.Combine(root, stored));
        await using (var stream = System.IO.File.Create(path))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var document = new Document
        {
            WorkspaceId = workspaceId,
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = stored,
            StoragePath = path,
            SizeBytes = file.Length,
            Status = DocumentStatus.Queued
        };
        db.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);
        await processor.EnqueueAsync(document.Id, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = document.Id }, ToDto(document));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var document = await db.Documents
            .Include(x => x.Workspace)
            .SingleOrDefaultAsync(x => x.Id == id && x.Workspace!.UserId == CurrentUserId, cancellationToken);
        return document is null ? NotFound() : ToDto(document);
    }

    [HttpGet("{id:guid}/chunks")]
    public async Task<IReadOnlyList<DocumentChunkDto>> Chunks(Guid id, CancellationToken cancellationToken)
    {
        return await db.DocumentChunks
            .Where(x => x.DocumentId == id && x.Document!.Workspace!.UserId == CurrentUserId)
            // CreatedAt reflects the worker's sequential inserts, restoring the
            // original reading order for chunks within the same page.
            .OrderBy(x => x.PageNumber)
            .ThenBy(x => x.CreatedAt)
            .Select(x => new DocumentChunkDto(x.Id, x.Text, x.PageNumber, x.SectionName))
            .ToListAsync(cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var document = await db.Documents.Include(x => x.Workspace).SingleOrDefaultAsync(x => x.Id == id && x.Workspace!.UserId == CurrentUserId, cancellationToken);
        if (document is null) return NotFound();
        db.Documents.Remove(document);
        await db.SaveChangesAsync(cancellationToken);
        await vectorStore.DeleteAsync(document.WorkspaceId, document.Id, cancellationToken);
        TryDeleteFile(document.StoragePath);
        return NoContent();
    }

    internal static void TryDeleteFile(string path)
    {
        try
        {
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best-effort: a locked or missing file must not fail the delete request.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static DocumentDto ToDto(Document x)
    {
        return new DocumentDto(x.Id, x.WorkspaceId, x.OriginalFileName, x.Status, x.Title, x.Authors, x.PublicationYear, x.Abstract, x.Keywords, x.CreatedAt);
    }
}

