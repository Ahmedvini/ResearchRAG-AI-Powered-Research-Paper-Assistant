using ResearchRag.Application.Abstractions;

namespace ResearchRag.Infrastructure.Ai;

public sealed class HashEmbeddingProvider : IEmbeddingProvider
{
    public Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        var vector = new float[384];
        foreach (var token in text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(token);
            var index = Math.Abs(hash % vector.Length);
            vector[index] += 1f;
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
}

