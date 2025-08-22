using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenAI.Embeddings;
using TiDB.Vector.Abstractions;

namespace TiDB.Vector.OpenAI.Embedding
{
    public sealed class OpenAIEmbeddingGenerator : IEmbeddingGenerator
    {
        private readonly EmbeddingClient _client;
        public int Dimension { get; }

        public OpenAIEmbeddingGenerator(string apiKey, string model, int dimension)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentNullException(nameof(apiKey));
            if (string.IsNullOrWhiteSpace(model)) throw new ArgumentNullException(nameof(model));
            _client = new EmbeddingClient(model, apiKey);
            Dimension = dimension;
        }

        public async Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
        {
            var options = new EmbeddingGenerationOptions { Dimensions = this.Dimension };
            var result = await _client.GenerateEmbeddingAsync(text, options, cancellationToken).ConfigureAwait(false);
            return result.Value.ToFloats().ToArray();
        }

        public async Task<IReadOnlyList<float[]>> GenerateBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
        {
            var inputs = texts as string[] ?? [.. texts];
            if (inputs.Length == 0) return Array.Empty<float[]>();

            var options = new EmbeddingGenerationOptions { Dimensions = this.Dimension };
            var result = await _client.GenerateEmbeddingsAsync(inputs, options, cancellationToken).ConfigureAwait(false);
            var list = new List<float[]>(inputs.Length);
            foreach (var e in result.Value)
            {
                list.Add(e.ToFloats().ToArray());
            }
            return list;
        }
    }
}


