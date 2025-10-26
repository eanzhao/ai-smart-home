using AISmartHome.Agents.Storage;

namespace AISmartHome.Agents.Tests.Mocks;

/// <summary>
/// Mock embedding service for testing - generates deterministic embeddings
/// </summary>
public class MockEmbeddingService : IEmbeddingService
{
    private readonly int _dimensions;
    private readonly Random _random = new(42); // Fixed seed for deterministic results

    public int EmbeddingDimension => _dimensions;

    public MockEmbeddingService(int dimensions = 1536)
    {
        _dimensions = dimensions;
    }

    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        // Generate deterministic embedding based on text hash
        var hash = text.GetHashCode();
        var localRandom = new Random(hash);
        
        var embedding = new float[_dimensions];
        for (int i = 0; i < _dimensions; i++)
        {
            embedding[i] = (float)(localRandom.NextDouble() * 2 - 1); // Range: -1 to 1
        }
        
        // Normalize to unit vector
        var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < _dimensions; i++)
            {
                embedding[i] /= magnitude;
            }
        }
        
        return Task.FromResult(embedding);
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts, CancellationToken ct = default)
    {
        var embeddings = new List<float[]>();
        foreach (var text in texts)
        {
            embeddings.Add(await GenerateEmbeddingAsync(text, ct));
        }
        return embeddings;
    }
}

