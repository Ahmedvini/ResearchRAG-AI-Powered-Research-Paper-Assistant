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

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
