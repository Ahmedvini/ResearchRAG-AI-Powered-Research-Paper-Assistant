using Microsoft.EntityFrameworkCore;
using ResearchRag.Application.Abstractions;
using ResearchRag.Domain.Entities;
using ResearchRag.Infrastructure.Persistence;

namespace ResearchRag.Infrastructure.Processing;

public sealed class DatabaseDocumentProcessorClient(AppDbContext db) : IDocumentProcessorClient
{
    public async Task EnqueueAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var exists = await db.ProcessingJobs.AnyAsync(x => x.DocumentId == documentId, cancellationToken);
        if (!exists)
        {
            db.ProcessingJobs.Add(new ProcessingJob { DocumentId = documentId });
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

