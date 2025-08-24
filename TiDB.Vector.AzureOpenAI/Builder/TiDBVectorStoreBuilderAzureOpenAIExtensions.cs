using System;
using TiDB.Vector.Abstractions;
using TiDB.Vector.AzureOpenAI.Chat;
using TiDB.Vector.AzureOpenAI.Embedding;
using TiDB.Vector.Core;

namespace TiDB.Vector.AzureOpenAI.Builder
{
    public static class TiDBVectorStoreBuilderAzureOpenAIExtensions
    {
        public static TiDBVectorStoreBuilder AddAzureOpenAITextEmbedding(
            this TiDBVectorStoreBuilder builder,
            string apiKey,
            string endpoint,
            string embeddingModel,
            int dimension
        )
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            var config = new AzureOpenAIConfig
            {
                ApiType = ApiType.Embedding,
                Auth = AuthType.APIKey,
                ApiKey = apiKey,
                Endpoint = endpoint,
                DeploymentName = embeddingModel,
                EmbeddingDimension = dimension,
            };

            IEmbeddingGenerator generator = new AzureOpenAIEmbeddingGenerator(config);
            return builder.UseEmbeddingGenerator(generator);
        }

        public static TiDBVectorStoreBuilder AddAzureOpenAIChatCompletion(
            this TiDBVectorStoreBuilder builder,
            string apiKey,
            string endpoint,
            string chatModel
        )
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            var config = new AzureOpenAIConfig
            {
                ApiType = ApiType.Chat,
                Auth = AuthType.APIKey,
                ApiKey = apiKey,
                Endpoint = endpoint,
                DeploymentName = chatModel,
            };

            ITextGenerator generator = new AzureOpenAITextGenerator(config);
            return builder.UseTextGenerator(generator);
        }
    }
}
