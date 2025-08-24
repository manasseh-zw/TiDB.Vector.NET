using System;
using TiDB.Vector.Abstractions;
using TiDB.Vector.AzureOpenAI.Chat;
using TiDB.Vector.AzureOpenAI.Embedding;
using TiDB.Vector.Core;

namespace TiDB.Vector.AzureOpenAI.Builder
{
    public static class TiDBVectorStoreBuilderAzureOpenAIExtensions
    {
        /// <summary>
        /// Configures Azure OpenAI API key and endpoint
        /// </summary>
        public static TiDBVectorStoreBuilder AddAzureOpenAI(
            this TiDBVectorStoreBuilder builder,
            string apiKey,
            string endpoint)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API key cannot be empty", nameof(apiKey));
            if (string.IsNullOrWhiteSpace(endpoint)) throw new ArgumentException("Endpoint cannot be empty", nameof(endpoint));
            
            builder.SetAzureOpenAIConfig(apiKey, endpoint);
            return builder;
        }

        /// <summary>
        /// Adds Azure OpenAI text embedding using the configured API key and endpoint
        /// </summary>
        public static TiDBVectorStoreBuilder AddAzureOpenAITextEmbedding(
            this TiDBVectorStoreBuilder builder,
            string embeddingModel,
            int dimension)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            
            var apiKey = builder.GetAzureOpenAIApiKey();
            var endpoint = builder.GetAzureOpenAIEndpoint();
            
            if (apiKey == null || endpoint == null)
            {
                throw new InvalidOperationException("Call AddAzureOpenAI() first to configure the API key and endpoint");
            }

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

        /// <summary>
        /// Adds Azure OpenAI chat completion using the configured API key and endpoint
        /// </summary>
        public static TiDBVectorStoreBuilder AddAzureOpenAIChatCompletion(
            this TiDBVectorStoreBuilder builder,
            string chatModel)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            
            var apiKey = builder.GetAzureOpenAIApiKey();
            var endpoint = builder.GetAzureOpenAIEndpoint();
            
            if (apiKey == null || endpoint == null)
            {
                throw new InvalidOperationException("Call AddAzureOpenAI() first to configure the API key and endpoint");
            }

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

        // Legacy methods for backward compatibility
        /// <summary>
        /// Legacy method - use AddAzureOpenAI() + AddAzureOpenAITextEmbedding() instead
        /// </summary>
        [Obsolete("Use AddAzureOpenAI() + AddAzureOpenAITextEmbedding() instead")]
        public static TiDBVectorStoreBuilder AddAzureOpenAITextEmbedding(
            this TiDBVectorStoreBuilder builder,
            string apiKey,
            string endpoint,
            string embeddingModel,
            int dimension)
        {
            return builder.AddAzureOpenAI(apiKey, endpoint).AddAzureOpenAITextEmbedding(embeddingModel, dimension);
        }

        /// <summary>
        /// Legacy method - use AddAzureOpenAI() + AddAzureOpenAIChatCompletion() instead
        /// </summary>
        [Obsolete("Use AddAzureOpenAI() + AddAzureOpenAIChatCompletion() instead")]
        public static TiDBVectorStoreBuilder AddAzureOpenAIChatCompletion(
            this TiDBVectorStoreBuilder builder,
            string apiKey,
            string endpoint,
            string chatModel)
        {
            return builder.AddAzureOpenAI(apiKey, endpoint).AddAzureOpenAIChatCompletion(chatModel);
        }
    }
}
