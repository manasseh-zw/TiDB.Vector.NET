using System;
using TiDB.Vector.Abstractions;
using TiDB.Vector.Core;
using TiDB.Vector.OpenAI.Chat;
using TiDB.Vector.OpenAI.Embedding;

namespace TiDB.Vector.OpenAI.Builder;

public static class TiDBVectorStoreBuilderOpenAIExtensions
{
    /// <summary>
    /// Configures OpenAI API key and optional custom endpoint
    /// </summary>
    public static TiDBVectorStoreBuilder AddOpenAI(
        this TiDBVectorStoreBuilder builder,
        string apiKey,
        string? endpoint = null)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API key cannot be empty", nameof(apiKey));
        
        builder.SetOpenAIConfig(apiKey, endpoint);
        return builder;
    }

    /// <summary>
    /// Adds OpenAI text embedding using the configured API key
    /// </summary>
    public static TiDBVectorStoreBuilder AddOpenAITextEmbedding(
        this TiDBVectorStoreBuilder builder,
        string embeddingModel,
        int? dimension = null)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        
        var apiKey = builder.GetOpenAIApiKey();
        var endpoint = builder.GetOpenAIEndpoint();
        
        if (apiKey == null)
        {
            throw new InvalidOperationException("Call AddOpenAI() first to configure the API key");
        }

        OpenAIConfig config;
        if (!string.IsNullOrEmpty(endpoint))
        {
            config = OpenAIConfig.ForCustomEndpoint(apiKey, endpoint, embeddingModel, dimension);
        }
        else
        {
            config = OpenAIConfig.ForOpenAI(apiKey, embeddingModel, dimension);
        }
        
        IEmbeddingGenerator generator = new OpenAIEmbeddingGenerator(config);
        return builder.UseEmbeddingGenerator(generator);
    }

    /// <summary>
    /// Adds OpenAI chat completion using the configured API key
    /// </summary>
    public static TiDBVectorStoreBuilder AddOpenAIChatCompletion(
        this TiDBVectorStoreBuilder builder,
        string chatModel)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        
        var apiKey = builder.GetOpenAIApiKey();
        var endpoint = builder.GetOpenAIEndpoint();
        
        if (apiKey == null)
        {
            throw new InvalidOperationException("Call AddOpenAI() first to configure the API key");
        }

        OpenAIConfig config;
        if (!string.IsNullOrEmpty(endpoint))
        {
            config = OpenAIConfig.ForCustomEndpoint(apiKey, endpoint, chatModel);
        }
        else
        {
            config = OpenAIConfig.ForOpenAI(apiKey, chatModel);
        }
        
        ITextGenerator generator = new OpenAITextGenerator(config);
        return builder.UseTextGenerator(generator);
    }

    // Legacy methods for backward compatibility
    /// <summary>
    /// Legacy method - use AddOpenAI() + AddOpenAITextEmbedding() instead
    /// </summary>
    [Obsolete("Use AddOpenAI() + AddOpenAITextEmbedding() instead")]
    public static TiDBVectorStoreBuilder AddOpenAITextEmbedding(
        this TiDBVectorStoreBuilder builder,
        string apiKey,
        string embeddingModel,
        int? dimension = null)
    {
        return builder.AddOpenAI(apiKey).AddOpenAITextEmbedding(embeddingModel, dimension);
    }

    /// <summary>
    /// Legacy method - use AddOpenAI() + AddOpenAIChatCompletion() instead
    /// </summary>
    [Obsolete("Use AddOpenAI() + AddOpenAIChatCompletion() instead")]
    public static TiDBVectorStoreBuilder AddOpenAIChatCompletion(
        this TiDBVectorStoreBuilder builder,
        string apiKey,
        string chatModel)
    {
        return builder.AddOpenAI(apiKey).AddOpenAIChatCompletion(chatModel);
    }

    /// <summary>
    /// Legacy method - use AddOpenAI() + AddOpenAITextEmbedding() instead
    /// </summary>
    [Obsolete("Use AddOpenAI() + AddOpenAITextEmbedding() instead")]
    public static TiDBVectorStoreBuilder AddOpenAITextEmbedding(
        this TiDBVectorStoreBuilder builder,
        string apiKey,
        string endpoint,
        string embeddingModel,
        int? dimension = null)
    {
        return builder.AddOpenAI(apiKey, endpoint).AddOpenAITextEmbedding(embeddingModel, dimension);
    }

    /// <summary>
    /// Legacy method - use AddOpenAI() + AddOpenAIChatCompletion() instead
    /// </summary>
    [Obsolete("Use AddOpenAI() + AddOpenAIChatCompletion() instead")]
    public static TiDBVectorStoreBuilder AddOpenAIChatCompletion(
        this TiDBVectorStoreBuilder builder,
        string apiKey,
        string endpoint,
        string chatModel)
    {
        return builder.AddOpenAI(apiKey, endpoint).AddOpenAIChatCompletion(chatModel);
    }
}
