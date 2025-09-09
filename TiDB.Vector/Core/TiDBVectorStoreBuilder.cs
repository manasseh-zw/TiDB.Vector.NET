using System;
using TiDB.Vector.Abstractions;

namespace TiDB.Vector.Core
{
    public sealed class TiDBVectorStoreBuilder
    {
        private readonly string _connectionString;
        private string _defaultCollection = "default";
        private string _tableName = "tidb_vectors";
        private DistanceFunction _distanceFunction = DistanceFunction.Cosine;
        private int _embeddingDimension = 1568; // default per project decision
        private IEmbeddingGenerator? _embeddingGenerator;
        private ITextGenerator? _textGenerator;
        private bool _ensureSchema;
        private bool _createVectorIndex;

        // OpenAI configuration
        private string? _openAIApiKey;
        private string? _openAIEndpoint;

        // Azure OpenAI configuration
        private string? _azureOpenAIApiKey;
        private string? _azureOpenAIEndpoint;

        public TiDBVectorStoreBuilder(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public TiDBVectorStoreBuilder WithDefaultCollection(string collection)
        {
            _defaultCollection = string.IsNullOrWhiteSpace(collection) ? _defaultCollection : collection;
            return this;
        }

        public TiDBVectorStoreBuilder WithTableName(string tableName)
        {
            if (!string.IsNullOrWhiteSpace(tableName))
                _tableName = tableName.Trim();
            return this;
        }

        public TiDBVectorStoreBuilder WithDistanceFunction(DistanceFunction distance)
        {
            _distanceFunction = distance;
            return this;
        }

        public TiDBVectorStoreBuilder UseEmbeddingGenerator(IEmbeddingGenerator generator)
        {
            _embeddingGenerator = generator ?? throw new ArgumentNullException(nameof(generator));
            _embeddingDimension = generator.Dimension;
            return this;
        }

        public TiDBVectorStoreBuilder UseTextGenerator(ITextGenerator generator)
        {
            _textGenerator = generator ?? throw new ArgumentNullException(nameof(generator));
            return this;
        }

        public TiDBVectorStoreBuilder EnsureSchema(bool createVectorIndex)
        {
            _ensureSchema = true;
            _createVectorIndex = createVectorIndex;
            return this;
        }

        public TiDBVectorStore Build()
        {
            return new TiDBVectorStore(
                _connectionString,
                _defaultCollection,
                _tableName,
                _distanceFunction,
                _embeddingDimension,
                _embeddingGenerator,
                _textGenerator,
                _ensureSchema,
                _createVectorIndex);
        }

        // OpenAI configuration methods
        internal string? GetOpenAIApiKey() => _openAIApiKey;
        internal string? GetOpenAIEndpoint() => _openAIEndpoint;
        internal void SetOpenAIConfig(string apiKey, string? endpoint = null)
        {
            _openAIApiKey = apiKey;
            _openAIEndpoint = endpoint;
        }

        // Azure OpenAI configuration methods
        public string? GetAzureOpenAIApiKey() => _azureOpenAIApiKey;
        public string? GetAzureOpenAIEndpoint() => _azureOpenAIEndpoint;
        public void SetAzureOpenAIConfig(string apiKey, string endpoint)
        {
            _azureOpenAIApiKey = apiKey;
            _azureOpenAIEndpoint = endpoint;
        }
    }
}


