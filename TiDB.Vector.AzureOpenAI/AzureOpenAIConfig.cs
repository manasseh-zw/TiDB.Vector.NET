namespace TiDB.Vector.AzureOpenAI;

public class AzureOpenAIConfig
{
    internal ApiType ApiType { get; set; } = ApiType.Unknown;
    
    public AuthType Auth { get; set; } = AuthType.Unknown;
    public required string ApiKey { get; set; }
    public required string Endpoint { get; set; }
    public required string DeploymentName { get; set; }
    public int EmbeddingDimension { get; set; }

    public void Validate()
    {
        if (Auth == AuthType.Unknown)
        {
            throw new Exception(
                $"Azure OpenAI: authentication type is not defined you can either use APIKEY or AZURE_IDENTITY"
            );
        }

        if (this.Auth == AuthType.APIKey && string.IsNullOrWhiteSpace(this.ApiKey))
        {
            throw new Exception($"Azure OpenAI: {nameof(ApiKey)} is empty");
        }

        if (string.IsNullOrWhiteSpace(Endpoint))
        {
            throw new Exception($"Azure OpenAI: {nameof(Endpoint)} is empty");
        }

        if (!Endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"Azure OpenAI: {nameof(Endpoint)} must start with https://");
        }

        if (string.IsNullOrWhiteSpace(DeploymentName))
        {
            throw new Exception(
                $"Azure OpenAI: {nameof(DeploymentName)} (deployment name) is empty"
            );
        }
        
        // Only validate embedding dimension when using embeddings
        if (ApiType == ApiType.Embedding && EmbeddingDimension is < 1)
        {
            throw new Exception($"Azure OpenAI: {nameof(EmbeddingDimension)} must be at least 1");
        }
    }
}

public enum AuthType
{
    Unknown = -1,
    APIKey,
}

internal enum ApiType
{
    Unknown = -1,
    Embedding,
    Chat,
}
