using System.ClientModel;
using OpenAI.Embeddings;

namespace AISmartHome.Agents.Storage;

/// <summary>
/// Embedding service using OpenAI's text-embedding models
/// Default model: text-embedding-3-small (1536 dimensions, fast and cost-effective)
/// </summary>
public class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _embeddingClient;
    private readonly string _model;
    private readonly int _dimensions;

    public OpenAIEmbeddingService(string apiKey, string? endpoint = null, string model = "text-embedding-3-small")
    {
        _model = model;
        
        var options = endpoint != null 
            ? new OpenAI.OpenAIClientOptions { Endpoint = new Uri(endpoint) }
            : new OpenAI.OpenAIClientOptions();
        
        _embeddingClient = new EmbeddingClient(_model, new ApiKeyCredential(apiKey), options);
        
        // Set dimensions based on model
        _dimensions = model switch
        {
            "text-embedding-3-small" => 1536,
            "text-embedding-3-large" => 3072,
            "text-embedding-ada-002" => 1536,
            _ => 1536
        };
        
        Console.WriteLine($"[OpenAIEmbeddingService] Initialized with model: {_model}, dimensions: {_dimensions}");
    }

    public int EmbeddingDimension => _dimensions;

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Console.WriteLine("[OpenAIEmbeddingService] Warning: Empty text, returning zero vector");
            return new float[_dimensions];
        }

        try
        {
            Console.WriteLine($"[OpenAIEmbeddingService] Generating embedding for text (length: {text.Length})");
            
            var response = await _embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: ct);
            var embedding = response.Value.ToFloats().ToArray();
            
            Console.WriteLine($"[OpenAIEmbeddingService] Generated embedding with {embedding.Length} dimensions");
            
            return embedding;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] OpenAIEmbeddingService failed: {ex.Message}");
            throw;
        }
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts, CancellationToken ct = default)
    {
        if (texts == null || texts.Count == 0)
        {
            return new List<float[]>();
        }

        Console.WriteLine($"[OpenAIEmbeddingService] Generating embeddings for {texts.Count} texts (batch)");
        
        // For now, process sequentially
        // TODO: Can optimize with actual batch API if needed
        var embeddings = new List<float[]>();
        
        foreach (var text in texts)
        {
            var embedding = await GenerateEmbeddingAsync(text, ct);
            embeddings.Add(embedding);
        }
        
        Console.WriteLine($"[OpenAIEmbeddingService] Generated {embeddings.Count} embeddings");
        
        return embeddings;
    }
}

