using Microsoft.EntityFrameworkCore;
using ResearchRag.Application.Abstractions;
using ResearchRag.Application.Services;
using ResearchRag.Infrastructure.Persistence;

namespace ResearchRag.Infrastructure.Retrieval;

public sealed class RetrievalService(
    AppDbContext db,
    IEmbeddingProvider embeddingProvider,
    IVectorStore vectorStore,
    IRerankerProvider rerankerProvider) : IRetrievalService
{
    public async Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(Guid workspaceId, IReadOnlyList<Guid>? documentIds, string query, int topK, CancellationToken cancellationToken)
    {
        var baseQuery = db.DocumentChunks
            .Include(x => x.Document)
            .Where(x => x.WorkspaceId == workspaceId);

        if (documentIds is { Count: > 0 })
        {
            baseQuery = baseQuery.Where(x => documentIds.Contains(x.DocumentId));
        }

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var keywordRows = await baseQuery
            .Where(x => terms.Any(term => x.Text.Contains(term)))
            .Take(topK * 3)
            .ToListAsync(cancellationToken);

        var keywordHits = keywordRows
            .Select(chunk =>
            {
                var score = terms.Count(term => chunk.Text.Contains(term, StringComparison.OrdinalIgnoreCase)) / Math.Max(1.0, terms.Length);
                return ToRetrievedChunk(chunk, semanticScore: 0, keywordScore: score, combinedScore: score);
            })
            .ToList();

        var vector = await embeddingProvider.EmbedAsync(query, cancellationToken);
        var vectorHitsRaw = await vectorStore.SearchAsync(workspaceId, documentIds, vector, topK * 3, cancellationToken);
        var vectorIds = vectorHitsRaw.Select(x => x.ChunkId).ToHashSet();
        var vectorRows = await baseQuery.Where(x => vectorIds.Contains(x.Id)).ToListAsync(cancellationToken);
        var vectorScores = vectorHitsRaw.ToDictionary(x => x.ChunkId, x => x.Score);
        var vectorHits = vectorRows.Select(chunk => ToRetrievedChunk(chunk, vectorScores.GetValueOrDefault(chunk.Id), 0, vectorScores.GetValueOrDefault(chunk.Id))).ToList();

        var merged = HybridScoreMerger.Merge(keywordHits, vectorHits).Take(topK * 2).ToList();
        return await rerankerProvider.RerankAsync(query, merged, topK, cancellationToken);
    }

    private static RetrievedChunk ToRetrievedChunk(Domain.Entities.DocumentChunk chunk, double semanticScore, double keywordScore, double combinedScore)
    {
        return new RetrievedChunk(
            chunk.Id,
            chunk.DocumentId,
            chunk.Document?.OriginalFileName ?? "Unknown document",
            chunk.Text,
            chunk.SectionName,
            chunk.PageNumber,
            semanticScore,
            keywordScore,
            combinedScore);
    }
}

