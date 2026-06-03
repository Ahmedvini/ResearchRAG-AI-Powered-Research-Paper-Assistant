namespace ResearchRag.Application.Abstractions;

public sealed record VectorSearchHit(Guid ChunkId, double Score);

public sealed record RetrievedChunk(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentName,
    string Text,
    string Section,
    int PageNumber,
    double SemanticScore,
    double KeywordScore,
    double CombinedScore);

