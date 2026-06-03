using ResearchRag.Domain.Common;
using ResearchRag.Domain.Enums;

namespace ResearchRag.Domain.Entities;

public sealed class ProcessingJob : Entity
{
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
    public ProcessingJobStatus Status { get; set; } = ProcessingJobStatus.Queued;
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

