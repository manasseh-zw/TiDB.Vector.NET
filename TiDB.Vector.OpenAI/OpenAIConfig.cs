namespace TiDB.Vector.OpenAI;

public class OpenAIConfig
{
    internal ApiType ApiType { get; set; } = ApiType.Unknown;
    
    public required string ApiKey { get; set; }
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
}

internal enum ApiType
{
    Unknown = -1,
    Embedding,
    Chat,
}