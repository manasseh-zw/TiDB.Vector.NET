using System;
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
            string embeddingModel,
            int? dimension = null)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            var dim = dimension ?? 1536; // default to text-embedding-3-small unless overridden
            
            var config = new OpenAIConfig
            {
                ApiType = ApiType.Embedding,
                ApiKey = apiKey,
                Model = embeddingModel,
                Dimension = dim
            };
            
            IEmbeddingGenerator generator = new OpenAIEmbeddingGenerator(config);
            return builder.UseEmbeddingGenerator(generator);
        }

        public static TiDBVectorStoreBuilder AddOpenAIChatCompletion(
            this TiDBVectorStoreBuilder builder,
            string apiKey,
            string chatModel)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            
            var config = new OpenAIConfig
            {
                ApiType = ApiType.Chat,
                ApiKey = apiKey,
                Model = chatModel
            };
            
            ITextGenerator generator = new OpenAITextGenerator(config);
            return builder.UseTextGenerator(generator);
        }
    }
}
