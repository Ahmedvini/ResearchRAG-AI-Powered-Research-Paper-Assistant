using Microsoft.EntityFrameworkCore;
using ResearchRag.Application.Abstractions;
using ResearchRag.Domain.Entities;
using ResearchRag.Domain.Enums;
using ResearchRag.Infrastructure.Persistence;

namespace ResearchRag.Infrastructure.Processing;

public sealed class DatabaseDocumentProcessorClient(AppDbContext db) : IDocumentProcessorClient
{
    public async Task EnqueueAsync(Guid documentId, CancellationToken cancellationToken)
    {
        // Only an already-pending job blocks a new enqueue; Failed and Completed
        // jobs do not, so documents remain re-processable after failures.
        var pending = await db.ProcessingJobs.AnyAsync(
            x => x.DocumentId == documentId &&
                 (x.Status == ProcessingJobStatus.Queued || x.Status == ProcessingJobStatus.Running),
            cancellationToken);
        if (!pending)
        {
            db.ProcessingJobs.Add(new ProcessingJob { DocumentId = documentId });
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

