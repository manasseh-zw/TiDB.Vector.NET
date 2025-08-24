using OpenAI.Embeddings;
using TiDB.Vector.Abstractions;
using OpenAI;
using System.ClientModel;

namespace TiDB.Vector.OpenAI.Embedding;

public sealed class OpenAIEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly EmbeddingClient _client;
    public int Dimension { get; }

    public OpenAIEmbeddingGenerator(OpenAIConfig config)
    {
        config.ApiType = ApiType.Embedding;
        config.Validate();
        
        if (!config.Dimension.HasValue)
        {
            throw new ArgumentException("Dimension must be specified for embedding generation", nameof(config));
        }

        var apiKeyCredential = new ApiKeyCredential(config.ApiKey);

        // Create client with custom endpoint if provided, otherwise use OpenAI's default
        if (!string.IsNullOrEmpty(config.Endpoint))
        {
            var options = new OpenAIClientOptions
            {
                Endpoint = new Uri(config.Endpoint)
            };
            var openAIClient = new OpenAIClient(apiKeyCredential, options);
            _client = openAIClient.GetEmbeddingClient(config.Model);
        }
        else
        {
            var openAIClient = new OpenAIClient(apiKeyCredential);
            _client = openAIClient.GetEmbeddingClient(config.Model);
        }
        
        Dimension = config.Dimension.Value;
    }

    public async Task<float[]> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default
    )
    {
        var options = new EmbeddingGenerationOptions { Dimensions = this.Dimension };
        var result = await _client
            .GenerateEmbeddingAsync(text, options, cancellationToken)
            .ConfigureAwait(false);
        return result.Value.ToFloats().ToArray();
    }

    public async Task<IReadOnlyList<float[]>> GenerateBatchAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default
    )
    {
        var inputs = texts as string[] ?? texts.ToArray();
        if (inputs.Length == 0)
            return Array.Empty<float[]>();

        var options = new EmbeddingGenerationOptions { Dimensions = this.Dimension };
        var result = await _client
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
