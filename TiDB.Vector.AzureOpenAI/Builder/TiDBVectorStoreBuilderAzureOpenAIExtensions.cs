using System;
using TiDB.Vector.Abstractions;
using TiDB.Vector.Core;
using TiDB.Vector.AzureOpenAI.Chat;
using TiDB.Vector.AzureOpenAI.Embedding;

namespace TiDB.Vector.AzureOpenAI.Builder
{
    public static class TiDBVectorStoreBuilderAzureOpenAIExtensions
    {
        public static TiDBVectorStoreBuilder AddAzureOpenAITextEmbedding(
            this TiDBVectorStoreBuilder builder,
            string apiKey,
            string endpoint,
            string deploymentName,
            int dimension)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            
            var config = new AzureOpenAIConfig
            {
                Auth = AuthType.APIKey,
                ApiKey = apiKey,
                Endpoint = endpoint,
                DeploymentName = deploymentName,
                EmbeddingDimension = dimension
            };
            
            IEmbeddingGenerator generator = new AzureOpenAIEmbeddingGenerator(config);
            return builder.UseEmbeddingGenerator(generator);
        }

        public static TiDBVectorStoreBuilder AddAzureOpenAIChatCompletion(
            this TiDBVectorStoreBuilder builder,
            string apiKey,
            string endpoint,
            string deploymentName)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            
            var config = new AzureOpenAIConfig
            {
                Auth = AuthType.APIKey,
                ApiKey = apiKey,
                Endpoint = endpoint,
                DeploymentName = deploymentName
            };
            
            ITextGenerator generator = new AzureOpenAITextGenerator(config);
            return builder.UseTextGenerator(generator);
        }
    }
}