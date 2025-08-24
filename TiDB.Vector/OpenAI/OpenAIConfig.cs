namespace TiDB.Vector.OpenAI;

public class OpenAIConfig
{
    internal ApiType ApiType { get; set; } = ApiType.Unknown;
    
    public required string ApiKey { get; set; }
    public string? Endpoint { get; set; } // null = use OpenAI's default endpoint
    public required string Model { get; set; }
    public int? Dimension { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new Exception($"OpenAI: {nameof(ApiKey)} is empty");
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            throw new Exception($"OpenAI: {nameof(Model)} is empty");
        }

        // Only validate embedding dimension when using embeddings
        if (ApiType == ApiType.Embedding && Dimension.HasValue && Dimension.Value < 1)
        {
            throw new Exception($"OpenAI: {nameof(Dimension)} must be at least 1");
        }
    }

    /// <summary>
    /// Creates a configuration for direct OpenAI usage
    /// </summary>
    public static OpenAIConfig ForOpenAI(string apiKey, string model, int? dimension = null)
    {
        return new OpenAIConfig
        {
            ApiKey = apiKey,
            Model = model,
            Dimension = dimension,
            Endpoint = null // Use OpenAI's default endpoint
        };
    }

    /// <summary>
    /// Creates a configuration for OpenAI-compatible endpoints (e.g., local models, other providers)
    /// </summary>
    public static OpenAIConfig ForCustomEndpoint(string apiKey, string endpoint, string model, int? dimension = null)
    {
        if (!endpoint.StartsWith("http"))
        {
            throw new ArgumentException("Endpoint must be a valid URL", nameof(endpoint));
        }

        return new OpenAIConfig
        {
            ApiKey = apiKey,
            Endpoint = endpoint,
            Model = model,
            Dimension = dimension
        };
    }
}

internal enum ApiType
{
    Unknown = -1,
    Embedding,
    Chat,
}
