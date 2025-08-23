using TiDB.Vector.Abstractions;
using TiDB.Vector.Core;
using TiDB.Vector.OpenAI.Chat;
using TiDB.Vector.OpenAI.Embedding;

namespace TiDB.Vector.OpenAI.Builder
{
    public static class TiDBVectorStoreBuilderOpenAIExtensions
    {
        public static TiDBVectorStoreBuilder AddOpenAITextEmbedding(
            this TiDBVectorStoreBuilder builder,
            string apiKey,
            string model,
            int? dimension = null
        )
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));
            var dim = dimension ?? 1536; // default to text-embedding-3-small unless overridden
            IEmbeddingGenerator generator = new OpenAIEmbeddingGenerator(apiKey, model, dim);
            return builder.UseEmbeddingGenerator(generator);
        }

        public static TiDBVectorStoreBuilder AddOpenAIChatCompletion(
            this TiDBVectorStoreBuilder builder,
            string apiKey,
            string model
        )
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));
            ITextGenerator generator = new OpenAITextGenerator(apiKey, model);
            return builder.UseTextGenerator(generator);
        }
    }
}
