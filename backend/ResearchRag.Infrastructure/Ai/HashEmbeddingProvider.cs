using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using ResearchRag.Application.Abstractions;

namespace ResearchRag.Infrastructure.Ai;

public sealed class HashEmbeddingProvider : IEmbeddingProvider
{
    public Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        var vector = new float[384];
        foreach (var token in text.ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            vector[HashBucket(token, vector.Length)] += 1f;
        }

        var magnitude = Math.Sqrt(vector.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] = (float)(vector[i] / magnitude);
            }
        }

        return Task.FromResult<IReadOnlyList<float>>(vector);
    }

    // Deterministic token bucket shared with the worker's HashEmbeddingProvider
    // (worker/src/researchrag_worker/embeddings.py). Both sides use the first
    // 4 bytes of MD5 (big-endian) mod dimension so that query vectors from the
    // API land in the same vector space as chunk vectors from the worker.
    // string.GetHashCode is randomized per process and must not be used here.
    internal static int HashBucket(string token, int dimension)
    {
        var digest = MD5.HashData(Encoding.UTF8.GetBytes(token));
        return (int)(BinaryPrimitives.ReadUInt32BigEndian(digest) % (uint)dimension);
    }
}
