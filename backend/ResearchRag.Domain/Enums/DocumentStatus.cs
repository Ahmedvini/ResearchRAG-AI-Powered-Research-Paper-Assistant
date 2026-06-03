namespace ResearchRag.Domain.Enums;

public enum DocumentStatus
{
    Queued,
    Extracting,
    Chunking,
    Embedding,
    Ready,
    Failed
}

