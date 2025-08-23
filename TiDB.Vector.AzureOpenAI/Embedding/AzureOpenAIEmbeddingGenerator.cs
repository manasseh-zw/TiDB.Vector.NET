using System.ClientModel;
using Azure.AI.OpenAI;
using OpenAI.Embeddings;
using TiDB.Vector.Abstractions;

namespace TiDB.Vector.AzureOpenAI.Embedding;

public class AzureOpenAIEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly AzureOpenAIClient _client;
    private readonly EmbeddingClient _embeddingClient;

    public AzureOpenAIEmbeddingGenerator(AzureOpenAIConfig config)
    {
        config.Validate();
        Dimension = config.EmbeddingDimension;
        _client = config.Auth switch
        {
            AuthType.APIKey => new AzureOpenAIClient(
                new Uri(config.Endpoint),
                new ApiKeyCredential(config.ApiKey)
            ),
            _ => throw new NotSupportedException(
                $"Authentication type '{config.Auth}' is not supported."
            ),
        };

        _embeddingClient = _client.GetEmbeddingClient(config.DeploymentName);
    }

    public int Dimension { get; }

    public async Task<float[]> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default
    )
    {
        var options = new EmbeddingGenerationOptions { Dimensions = this.Dimension };
        var result = await _embeddingClient
            .GenerateEmbeddingAsync(text, options, cancellationToken)
            .ConfigureAwait(false);
        return result.Value.ToFloats().ToArray();
    }

    public async Task<IReadOnlyList<float[]>> GenerateBatchAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default
    )
    {
        var inputs = texts as string[] ?? [.. texts];
        if (inputs.Length == 0)
            return Array.Empty<float[]>();

        var options = new EmbeddingGenerationOptions { Dimensions = this.Dimension };
        var result = await _embeddingClient
            .GenerateEmbeddingsAsync(inputs, options, cancellationToken)
            .ConfigureAwait(false);
        var list = new List<float[]>(inputs.Length);
        foreach (var e in result.Value)
        {
            list.Add(e.ToFloats().ToArray());
        }
        return list;
    }
}
