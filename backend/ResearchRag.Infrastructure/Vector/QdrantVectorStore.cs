using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using ResearchRag.Application.Abstractions;

namespace ResearchRag.Infrastructure.Vector;

public sealed class QdrantVectorStore(HttpClient httpClient, IConfiguration configuration) : IVectorStore
{
    public async Task<IReadOnlyList<VectorSearchHit>> SearchAsync(Guid workspaceId, IReadOnlyList<Guid>? documentIds, IReadOnlyList<float> vector, int topK, CancellationToken cancellationToken)
    {
        var collection = configuration["Qdrant:Collection"] ?? "researchrag_chunks";
        var filter = new
        {
            must = new List<object>
            {
                new { key = "workspace_id", match = new { value = workspaceId.ToString() } }
            }
        };

        if (documentIds is { Count: > 0 })
        {
            filter.must.Add(new { key = "document_id", match = new { any = documentIds.Select(x => x.ToString()).ToArray() } });
        }

        var request = new { vector, limit = topK, with_payload = true, filter };
        var response = await httpClient.PostAsJsonAsync($"/collections/{collection}/points/search", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var body = await response.Content.ReadFromJsonAsync<QdrantSearchResponse>(cancellationToken: cancellationToken);
        return body?.Result?
            .Where(x => x.Payload is not null && x.Payload.TryGetValue("chunk_id", out var chunkId) && Guid.TryParse(chunkId?.ToString(), out _))
            .Select(x => new VectorSearchHit(Guid.Parse(x.Payload!["chunk_id"]!.ToString()!), x.Score))
            .ToList() ?? [];
    }

    public async Task DeleteAsync(Guid workspaceId, Guid? documentId, CancellationToken cancellationToken)
    {
        var collection = configuration["Qdrant:Collection"] ?? "researchrag_chunks";
        var must = new List<object>
        {
            new { key = "workspace_id", match = new { value = workspaceId.ToString() } }
        };

        if (documentId is not null)
        {
            must.Add(new { key = "document_id", match = new { value = documentId.Value.ToString() } });
        }

        try
        {
            await httpClient.PostAsJsonAsync(
                $"/collections/{collection}/points/delete?wait=true",
                new { filter = new { must } },
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Best-effort cleanup: an unreachable vector store must not block
            // deleting the document or workspace itself.
        }
    }

    private sealed record QdrantSearchResponse(List<QdrantHit>? Result);
    private sealed record QdrantHit(double Score, Dictionary<string, object>? Payload);
}

