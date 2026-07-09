using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ResearchRag.Application.Abstractions;
using ResearchRag.Application.Research;
using ResearchRag.Domain.Entities;
using ResearchRag.Infrastructure.Persistence;

namespace ResearchRag.Infrastructure.Research;

public sealed class ResearchAnalysisService(AppDbContext db, ILLMProvider llmProvider) : IResearchAnalysisService
{
    public async Task<LiteratureReviewDto> GenerateLiteratureReviewAsync(Guid userId, LiteratureReviewRequest request, CancellationToken cancellationToken)
    {
        var documents = await LoadDocumentsAsync(userId, request.WorkspaceId, request.DocumentIds, cancellationToken);
        var chunks = await LoadRepresentativeChunksAsync(userId, request.WorkspaceId, request.DocumentIds, 18, cancellationToken);

        var background = SummarizeThemes(documents, chunks, "background");
        var methods = SummarizeThemes(documents, chunks, "method");
        var trends = SummarizeTrends(documents);
        var gaps = SummarizeThemes(documents, chunks, "limit");
        var future = SummarizeThemes(documents, chunks, "future");

        // When chunks are available, let the LLM write the full review as the
        // markdown deliverable; the per-section heuristics stay as the structured
        // fields instead of being overwritten by the whole generated text.
        string? generated = null;
        if (chunks.Count > 0)
        {
            var retrieved = chunks.Select(ToRetrievedChunk).ToList();
            generated = await llmProvider.GenerateAnswerAsync(
                "Generate a concise literature review with sections: Research Background, Existing Methods, Current Trends, Research Gaps, Future Work.",
                retrieved,
                cancellationToken);
        }

        var markdown = generated ?? new StringBuilder()
            .AppendLine("# Literature Review")
            .AppendLine()
            .AppendLine("## Research Background")
            .AppendLine(background)
            .AppendLine()
            .AppendLine("## Existing Methods")
            .AppendLine(methods)
            .AppendLine()
            .AppendLine("## Current Trends")
            .AppendLine(trends)
            .AppendLine()
            .AppendLine("## Research Gaps")
            .AppendLine(gaps)
            .AppendLine()
            .AppendLine("## Future Work")
            .AppendLine(future)
            .ToString();

        return new LiteratureReviewDto(background, methods, trends, gaps, future, markdown);
    }

    public async Task<PaperComparisonDto> ComparePapersAsync(Guid userId, PaperComparisonRequest request, CancellationToken cancellationToken)
    {
        var documents = await LoadDocumentsAsync(userId, request.WorkspaceId, request.DocumentIds, cancellationToken);
        var rows = documents.Select(document =>
        {
            var extraction = document.PaperExtraction;
            return new PaperComparisonRowDto(
                document.Id,
                document.Title ?? document.OriginalFileName,
                extraction?.Dataset ?? ExtractField(document, "dataset"),
                extraction?.Model ?? ExtractField(document, "model"),
                ExtractField(document, "method"),
                extraction?.MetricsJson is { Length: > 2 } ? string.Join(", ", ReadJsonList(extraction.MetricsJson)) : ExtractField(document, "metric"),
                extraction?.Accuracy ?? ExtractField(document, "result"),
                ExtractField(document, "strength"),
                extraction?.LimitationsJson is { Length: > 2 } ? string.Join(", ", ReadJsonList(extraction.LimitationsJson)) : ExtractField(document, "limit"));
        }).ToList();

        return new PaperComparisonDto(rows);
    }

    public async Task<ResearchGapDto> AnalyzeResearchGapsAsync(Guid userId, ResearchGapRequest request, CancellationToken cancellationToken)
    {
        var documents = await LoadDocumentsAsync(userId, request.WorkspaceId, null, cancellationToken);
        var chunks = await LoadRepresentativeChunksAsync(userId, request.WorkspaceId, null, 30, cancellationToken);

        var limitations = ExtractLines(chunks, ["limit", "limitation", "constraint"]).DefaultIfEmpty("No explicit limitations were found in the current processed chunks.").Take(6).ToList();
        var future = ExtractLines(chunks, ["future", "further", "next"]).DefaultIfEmpty("Add more ready papers or richer extracted methodology fields to identify future directions.").Take(6).ToList();
        var datasets = documents.Where(x => x.PaperExtraction?.Dataset is null or "").Select(x => x.Title ?? x.OriginalFileName).Take(6).Select(x => $"Dataset not extracted for {x}.").ToList();
        var evaluations = ExtractLines(chunks, ["evaluation", "benchmark", "metric"]).Take(6).ToList();
        if (evaluations.Count == 0)
        {
            evaluations.Add("Evaluation coverage is unclear from the current extracted chunks.");
        }

        var markdown = new StringBuilder()
            .AppendLine("# Research Gap Report")
            .AppendLine()
            .AppendLine("## Common Limitations")
            .AppendJoin("\n", limitations.Select(x => $"- {x}"))
            .AppendLine()
            .AppendLine("## Underexplored Areas")
            .AppendJoin("\n", future.Select(x => $"- {x}"))
            .AppendLine()
            .AppendLine("## Missing Datasets")
            .AppendJoin("\n", datasets.DefaultIfEmpty("No missing dataset signals were detected.").Select(x => $"- {x}"))
            .AppendLine()
            .AppendLine("## Missing Evaluations")
            .AppendJoin("\n", evaluations.Select(x => $"- {x}"))
            .ToString();

        return new ResearchGapDto(limitations, future, datasets, evaluations, markdown);
    }

    public async Task<KnowledgeGraphDto> GenerateKnowledgeGraphAsync(Guid userId, KnowledgeGraphRequest request, CancellationToken cancellationToken)
    {
        var documents = await LoadDocumentsAsync(userId, request.WorkspaceId, null, cancellationToken);
        var nodes = new Dictionary<string, KnowledgeGraphNodeDto>();
        var edges = new List<KnowledgeGraphEdgeDto>();

        foreach (var document in documents)
        {
            var paperId = $"paper:{document.Id}";
            AddNode(nodes, paperId, document.Title ?? document.OriginalFileName, "Paper");

            foreach (var author in SplitList(document.Authors))
            {
                var authorId = $"author:{Slug(author)}";
                AddNode(nodes, authorId, author, "Author");
                edges.Add(new KnowledgeGraphEdgeDto(authorId, paperId, "wrote"));
            }

            foreach (var keyword in SplitList(document.Keywords))
            {
                var topicId = $"topic:{Slug(keyword)}";
                AddNode(nodes, topicId, keyword, "Topic");
                edges.Add(new KnowledgeGraphEdgeDto(paperId, topicId, "covers"));
            }

            if (!string.IsNullOrWhiteSpace(document.PaperExtraction?.Dataset))
            {
                var datasetId = $"dataset:{Slug(document.PaperExtraction.Dataset)}";
                AddNode(nodes, datasetId, document.PaperExtraction.Dataset, "Dataset");
                edges.Add(new KnowledgeGraphEdgeDto(paperId, datasetId, "uses"));
            }

            if (!string.IsNullOrWhiteSpace(document.PaperExtraction?.Model))
            {
                var modelId = $"model:{Slug(document.PaperExtraction.Model)}";
                AddNode(nodes, modelId, document.PaperExtraction.Model, "Model");
                edges.Add(new KnowledgeGraphEdgeDto(paperId, modelId, "applies"));
            }
        }

        return new KnowledgeGraphDto(nodes.Values.ToList(), edges.Distinct().ToList());
    }

    public async Task<StudyToolsDto> GenerateStudyToolsAsync(Guid userId, StudyToolsRequest request, CancellationToken cancellationToken)
    {
        var documentIds = request.DocumentId is null ? null : new[] { request.DocumentId.Value };
        var count = Math.Clamp(request.Count, 3, 20);
        var chunks = await LoadRepresentativeChunksAsync(userId, request.WorkspaceId, documentIds, count, cancellationToken);

        var flashcards = chunks.Take(count).Select(chunk =>
            new FlashcardDto(
                $"What is a key point from {chunk.Document?.Title ?? chunk.Document?.OriginalFileName ?? "this paper"}?",
                Trim(chunk.Text, 360))).ToList();

        var quiz = chunks.Take(count).Select(chunk =>
            new QuizQuestionDto(
                "OpenEnded",
                $"Explain the idea discussed in {chunk.SectionName} on page {chunk.PageNumber}.",
                [],
                Trim(chunk.Text, 420))).ToList();

        return new StudyToolsDto(flashcards, quiz);
    }

    private async Task<List<Document>> LoadDocumentsAsync(Guid userId, Guid workspaceId, IReadOnlyList<Guid>? documentIds, CancellationToken cancellationToken)
    {
        IQueryable<Document> query = db.Documents
            .Include(x => x.Workspace)
            .Include(x => x.PaperExtraction)
            .Include(x => x.Chunks)
            .Where(x => x.WorkspaceId == workspaceId && x.Workspace!.UserId == userId);
        if (documentIds is { Count: > 0 })
        {
            query = query.Where(x => documentIds.Contains(x.Id));
        }

        return await query.OrderBy(x => x.PublicationYear ?? 9999).ThenBy(x => x.Title ?? x.OriginalFileName).ToListAsync(cancellationToken);
    }

    private async Task<List<DocumentChunk>> LoadRepresentativeChunksAsync(Guid userId, Guid workspaceId, IReadOnlyList<Guid>? documentIds, int count, CancellationToken cancellationToken)
    {
        IQueryable<DocumentChunk> query = db.DocumentChunks
            .Include(x => x.Document)
            .ThenInclude(x => x!.Workspace)
            .Where(x => x.WorkspaceId == workspaceId && x.Document!.Workspace!.UserId == userId);
        if (documentIds is { Count: > 0 })
        {
            query = query.Where(x => documentIds.Contains(x.DocumentId));
        }

        return await query.OrderBy(x => x.DocumentId).ThenBy(x => x.PageNumber).Take(count).ToListAsync(cancellationToken);
    }

    private static RetrievedChunk ToRetrievedChunk(DocumentChunk chunk)
    {
        return new RetrievedChunk(chunk.Id, chunk.DocumentId, chunk.Document?.OriginalFileName ?? "Unknown", chunk.Text, chunk.SectionName, chunk.PageNumber, 0, 1, 1);
    }

    private static string SummarizeThemes(IReadOnlyList<Document> documents, IReadOnlyList<DocumentChunk> chunks, string keyword)
    {
        var lines = ExtractLines(chunks, [keyword]).Take(4).ToList();
        if (lines.Count > 0) return string.Join(" ", lines);
        if (documents.Count == 0) return "No processed papers are available yet.";
        return $"Current workspace includes {documents.Count} paper(s): {string.Join(", ", documents.Take(5).Select(x => x.Title ?? x.OriginalFileName))}.";
    }

    private static string SummarizeTrends(IReadOnlyList<Document> documents)
    {
        var keywords = documents.SelectMany(x => SplitList(x.Keywords)).GroupBy(x => x, StringComparer.OrdinalIgnoreCase).OrderByDescending(x => x.Count()).Take(8).Select(x => x.Key).ToList();
        return keywords.Count == 0 ? "Trends will become clearer after metadata and keywords are extracted from more papers." : $"Recurring topics include {string.Join(", ", keywords)}.";
    }

    private static string ExtractField(Document document, string keyword)
    {
        var chunk = document.Chunks.FirstOrDefault(x => x.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        return chunk is null ? "Not extracted" : Trim(chunk.Text, 220);
    }

    private static IEnumerable<string> ExtractLines(IEnumerable<DocumentChunk> chunks, IReadOnlyList<string> keywords)
    {
        return chunks
            .SelectMany(chunk => chunk.Text.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Where(sentence => keywords.Any(keyword => sentence.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .Select(sentence => Trim(sentence, 240))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> ReadJsonList(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static IEnumerable<string> SplitList(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Take(12);
    }

    private static void AddNode(Dictionary<string, KnowledgeGraphNodeDto> nodes, string id, string label, string type)
    {
        nodes.TryAdd(id, new KnowledgeGraphNodeDto(id, label, type));
    }

    private static string Slug(string value)
    {
        return new string(value.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray()).Trim('-');
    }

    private static string Trim(string value, int maxLength)
    {
        value = string.Join(" ", value.Split()).Trim();
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}
