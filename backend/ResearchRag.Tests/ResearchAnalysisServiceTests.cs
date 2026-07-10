using Microsoft.EntityFrameworkCore;
using ResearchRag.Application.Research;
using ResearchRag.Domain.Entities;
using ResearchRag.Infrastructure.Ai;
using ResearchRag.Infrastructure.Persistence;
using ResearchRag.Infrastructure.Research;
using Xunit;

namespace ResearchRag.Tests;

public sealed class ResearchAnalysisServiceTests
{
    [Fact]
    public async Task ComparePapers_uses_extracted_metadata_and_chunks()
    {
        await using var db = CreateDb();
        var user = new User { Email = "researcher@example.com", DisplayName = "Researcher", PasswordHash = "hash" };
        var workspace = new Workspace { User = user, Name = "BCI", Description = "" };
        var document = new Document
        {
            Workspace = workspace,
            OriginalFileName = "paper.pdf",
            StoredFileName = "paper.pdf",
            StoragePath = "/tmp/paper.pdf",
            Title = "BCI Classifier"
        };
        document.PaperExtraction = new PaperExtraction
        {
            Document = document,
            Dataset = "EEG Motor Imagery",
            Model = "CNN",
            MetricsJson = "[\"Accuracy\"]"
        };
        document.Chunks.Add(new DocumentChunk
        {
            Document = document,
            WorkspaceId = workspace.Id,
            Text = "The methodology trains a CNN and reports strong results.",
            PageNumber = 2,
            SectionName = "Methodology"
        });
        db.Documents.Add(document);
        await db.SaveChangesAsync();

        var service = new ResearchAnalysisService(db, new EchoLlmProvider());
        var result = await service.ComparePapersAsync(user.Id, new PaperComparisonRequest(workspace.Id, [document.Id]), CancellationToken.None);

        Assert.Single(result.Rows);
        Assert.Equal("EEG Motor Imagery", result.Rows[0].Dataset);
        Assert.Equal("CNN", result.Rows[0].Model);
        Assert.Contains("methodology", result.Rows[0].Methodology, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task KnowledgeGraph_links_authors_topics_and_papers()
    {
        await using var db = CreateDb();
        var user = new User { Email = "researcher@example.com", DisplayName = "Researcher", PasswordHash = "hash" };
        var workspace = new Workspace { User = user, Name = "ML", Description = "" };
        db.Documents.Add(new Document
        {
            Workspace = workspace,
            OriginalFileName = "paper.pdf",
            StoredFileName = "paper.pdf",
            StoragePath = "/tmp/paper.pdf",
            Title = "Graph Learning",
            Authors = "Ada Lovelace; Alan Turing",
            Keywords = "graphs, learning"
        });
        await db.SaveChangesAsync();

        var service = new ResearchAnalysisService(db, new EchoLlmProvider());
        var graph = await service.GenerateKnowledgeGraphAsync(user.Id, new KnowledgeGraphRequest(workspace.Id), CancellationToken.None);

        Assert.Contains(graph.Nodes, node => node.Type == "Paper" && node.Label == "Graph Learning");
        Assert.Contains(graph.Nodes, node => node.Type == "Author" && node.Label == "Ada Lovelace");
        Assert.Contains(graph.Edges, edge => edge.Relation == "wrote");
    }

    [Fact]
    public async Task LiteratureReview_uses_llm_output_as_markdown_and_keeps_heuristic_sections()
    {
        await using var db = CreateDb();
        var (user, workspace, document) = SeedDocumentWithChunk(db);
        await db.SaveChangesAsync();

        var service = new ResearchAnalysisService(db, new EchoLlmProvider());
        var review = await service.GenerateLiteratureReviewAsync(user.Id, new LiteratureReviewRequest(workspace.Id, [document.Id]), CancellationToken.None);

        // With chunks available the LLM writes the markdown deliverable...
        Assert.Contains("Based on the retrieved paper excerpts", review.Markdown);
        // ...while the structured sections keep the heuristic summaries instead
        // of being overwritten by the full generated review.
        Assert.DoesNotContain("Based on the retrieved paper excerpts", review.Background);
    }

    [Fact]
    public async Task StudyTools_returns_items_even_when_requested_count_is_below_minimum()
    {
        await using var db = CreateDb();
        var (user, workspace, _) = SeedDocumentWithChunk(db);
        await db.SaveChangesAsync();

        var service = new ResearchAnalysisService(db, new EchoLlmProvider());
        var tools = await service.GenerateStudyToolsAsync(user.Id, new StudyToolsRequest(workspace.Id, null, 0), CancellationToken.None);

        Assert.NotEmpty(tools.Flashcards);
        Assert.NotEmpty(tools.Quiz);
    }

    private static (User User, Workspace Workspace, Document Document) SeedDocumentWithChunk(AppDbContext db)
    {
        var user = new User { Email = "researcher@example.com", DisplayName = "Researcher", PasswordHash = "hash" };
        var workspace = new Workspace { User = user, Name = "BCI", Description = "" };
        var document = new Document
        {
            Workspace = workspace,
            OriginalFileName = "paper.pdf",
            StoredFileName = "paper.pdf",
            StoragePath = "/tmp/paper.pdf",
            Title = "BCI Classifier"
        };
        document.Chunks.Add(new DocumentChunk
        {
            Document = document,
            WorkspaceId = workspace.Id,
            Text = "The background of this study covers brain computer interfaces.",
            PageNumber = 1,
            SectionName = "Introduction"
        });
        db.Documents.Add(document);
        return (user, workspace, document);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
