using ResearchRag.Application.Abstractions;
using ResearchRag.Application.Services;
using Xunit;

namespace ResearchRag.Tests;

public sealed class CitationFactoryTests
{
    [Fact]
    public void FromChunk_keeps_required_citation_fields()
    {
        var chunk = new RetrievedChunk(Guid.NewGuid(), Guid.NewGuid(), "paper.pdf", "text", "", 0, 0.4, 0.2, 0.61234);

        var citation = CitationFactory.FromChunk(chunk);

        Assert.Equal("paper.pdf", citation.DocumentName);
        Assert.Equal("Unknown", citation.Section);
        Assert.Equal(1, citation.PageNumber);
        Assert.Equal(0.6123, citation.RelevanceScore);
    }
}

