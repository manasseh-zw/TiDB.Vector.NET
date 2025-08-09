using System;
using TiDB.Vector.Abstractions;
using TiDB.Vector.Models;

namespace TiDB.Vector.Core
{
    public sealed class TiDBVectorStoreBuilder
    {
        private readonly string _connectionString;
        private string _defaultCollection = "default";
        private DistanceFunction _distanceFunction = DistanceFunction.Cosine;
        private int _embeddingDimension = 0;
        private IEmbeddingGenerator? _embeddingGenerator;
        private ITextGenerator? _textGenerator;
        private IChunker? _chunker;
        private ChunkingOptions _chunkingOptions = new();
        private bool _ensureSchema;
        private bool _createVectorIndex;

        public TiDBVectorStoreBuilder(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public TiDBVectorStoreBuilder WithDefaultCollection(string collection)
        {
            _defaultCollection = string.IsNullOrWhiteSpace(collection) ? _defaultCollection : collection;
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

        public TiDBVectorStoreBuilder UseChunker(IChunker chunker, ChunkingOptions? options = null)
        {
            _chunker = chunker ?? throw new ArgumentNullException(nameof(chunker));
            _chunkingOptions = options ?? _chunkingOptions;
            return this;
        }

        // Convenience fluent methods for OpenAI that we will flesh out in later iterations
        public TiDBVectorStoreBuilder AddOpenAI(string apiKey)
        {
            _ = apiKey; // placeholder for now
            return this;
        }

        public TiDBVectorStoreBuilder AddOpenAITextEmbedding(string model, int dimension)
        {
            _ = model; // placeholder
            _embeddingDimension = dimension;
            return this;
        }

        public TiDBVectorStoreBuilder AddOpenAIChatCompletion(string model)
        {
            _ = model; // placeholder
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
                _distanceFunction,
                _embeddingDimension,
                _embeddingGenerator,
                _textGenerator,
                _chunker,
                _chunkingOptions,
                _ensureSchema,
                _createVectorIndex);
        }
    }
}


