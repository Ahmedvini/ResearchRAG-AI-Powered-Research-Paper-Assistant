using ResearchRag.Application.Abstractions;
using ResearchRag.Application.Chats;

namespace ResearchRag.Application.Services;

public static class CitationFactory
{
    public static CitationDto FromChunk(RetrievedChunk chunk)
    {
        return new CitationDto(
            chunk.ChunkId,
            chunk.DocumentName,
            string.IsNullOrWhiteSpace(chunk.Section) ? "Unknown" : chunk.Section,
            Math.Max(1, chunk.PageNumber),
            Math.Round(chunk.CombinedScore, 4));
    }
}

