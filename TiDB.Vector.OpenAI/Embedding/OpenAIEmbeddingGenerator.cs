using OpenAI.Embeddings;
using TiDB.Vector.Abstractions;
using TiDB.Vector.OpenAI;

namespace TiDB.Vector.OpenAI.Embedding
{
    public sealed class OpenAIEmbeddingGenerator : IEmbeddingGenerator
    {
        private readonly EmbeddingClient _client;
        public int Dimension { get; }

        public OpenAIEmbeddingGenerator(OpenAIConfig config)
        {
            config.Validate();
            if (string.IsNullOrWhiteSpace(config.ApiKey))
                throw new ArgumentNullException(nameof(config.ApiKey));
            if (string.IsNullOrWhiteSpace(config.Model))
                throw new ArgumentNullException(nameof(config.Model));
            if (!config.Dimension.HasValue)
                throw new ArgumentException("Dimension must be specified for embedding generation", nameof(config.Dimension));
                
            _client = new EmbeddingClient(config.Model, config.ApiKey);
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
            var inputs = texts as string[] ?? [.. texts];
            if (inputs.Length == 0)
                return [];

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
}
