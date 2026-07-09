using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ResearchRag.Application.Abstractions;
using ResearchRag.Application.Services;
using ResearchRag.Domain.Entities;
using ResearchRag.Infrastructure.Persistence;

namespace ResearchRag.Infrastructure.Retrieval;

public sealed class RetrievalService(
    AppDbContext db,
    IEmbeddingProvider embeddingProvider,
    IVectorStore vectorStore,
    IRerankerProvider rerankerProvider) : IRetrievalService
{
    private const int MaxKeywordTerms = 16;

    public async Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(Guid workspaceId, IReadOnlyList<Guid>? documentIds, string query, int topK, CancellationToken cancellationToken)
    {
        var baseQuery = db.DocumentChunks
            .Include(x => x.Document)
            .Where(x => x.WorkspaceId == workspaceId);

        if (documentIds is { Count: > 0 })
        {
            baseQuery = baseQuery.Where(x => documentIds.Contains(x.DocumentId));
        }

        var terms = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxKeywordTerms)
            .ToArray();

        List<DocumentChunk> keywordRows = [];
        if (terms.Length > 0)
        {
            keywordRows = await baseQuery
                .Where(ContainsAnyTerm(terms))
                .OrderBy(x => x.Id)
                .Take(topK * 3)
                .ToListAsync(cancellationToken);
        }

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

    // Builds "chunk.Text.Contains(t1) || chunk.Text.Contains(t2) || ..." as an
    // expression tree. The previous "terms.Any(term => x.Text.Contains(term))"
    // form needs EF's parameterized-collection translation, which the MySQL
    // provider does not support; this form translates to plain LIKE clauses.
    private static Expression<Func<DocumentChunk, bool>> ContainsAnyTerm(IReadOnlyList<string> terms)
    {
        var parameter = Expression.Parameter(typeof(DocumentChunk), "chunk");
        var textProperty = Expression.Property(parameter, nameof(DocumentChunk.Text));
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

        Expression? body = null;
        foreach (var term in terms)
        {
            var call = Expression.Call(textProperty, containsMethod, Expression.Constant(term));
            body = body is null ? call : Expression.OrElse(body, call);
        }

        return Expression.Lambda<Func<DocumentChunk, bool>>(body ?? Expression.Constant(false), parameter);
    }

    private static RetrievedChunk ToRetrievedChunk(DocumentChunk chunk, double semanticScore, double keywordScore, double combinedScore)
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
