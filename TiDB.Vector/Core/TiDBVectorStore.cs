using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TiDB.Vector.Abstractions;
using TiDB.Vector.Models;
using TiDB.Vector.Options;

namespace TiDB.Vector.Core
{
    public sealed class TiDBVectorStore : IAsyncDisposable
    {
        private readonly string _connectionString;
        private readonly string _defaultCollection;
        private readonly DistanceFunction _distanceFunction;
        private readonly int _embeddingDimension;
        private readonly IEmbeddingGenerator? _embeddingGenerator;
        private readonly ITextGenerator? _textGenerator;
        private readonly IChunker? _chunker;
        private readonly ChunkingOptions _chunkingOptions;
        private readonly bool _ensureSchema;
        private readonly bool _createVectorIndex;

        internal TiDBVectorStore(
            string connectionString,
            string defaultCollection,
            DistanceFunction distanceFunction,
            int embeddingDimension,
            IEmbeddingGenerator? embeddingGenerator,
            ITextGenerator? textGenerator,
            IChunker? chunker,
            ChunkingOptions chunkingOptions,
            bool ensureSchema,
            bool createVectorIndex)
        {
            _connectionString = connectionString;
            _defaultCollection = defaultCollection;
            _distanceFunction = distanceFunction;
            _embeddingDimension = embeddingDimension;
            _embeddingGenerator = embeddingGenerator;
            _textGenerator = textGenerator;
            _chunker = chunker;
            _chunkingOptions = chunkingOptions;
            _ensureSchema = ensureSchema;
            _createVectorIndex = createVectorIndex;
        }

        public async Task EnsureSchemaAsync(bool? createVectorIndex = null, CancellationToken cancellationToken = default)
        {
            _ = createVectorIndex ?? _createVectorIndex;
            // Iteration 2 will implement actual DDL logic.
            await Task.CompletedTask;
        }

        public async Task UpsertAsync(
            UpsertItem item,
            UpsertOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            _ = item;
            _ = options;
            await Task.CompletedTask;
        }

        public async Task UpsertBatchAsync(
            IEnumerable<UpsertItem> items,
            UpsertOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            _ = items;
            _ = options;
            await Task.CompletedTask;
        }

        public async Task<IReadOnlyList<SearchResult>> SearchAsync(
            string query,
            int topK = 5,
            object? searchOptions = null,
            CancellationToken cancellationToken = default)
        {
            _ = query;
            _ = topK;
            _ = searchOptions;
            await Task.CompletedTask;
            return Array.Empty<SearchResult>();
        }

        public async Task<Answer> AskAsync(
            string query,
            int topK = 6,
            object? answerOptions = null,
            CancellationToken cancellationToken = default)
        {
            _ = query;
            _ = topK;
            _ = answerOptions;
            await Task.CompletedTask;
            return new Answer { Text = "Not implemented", Sources = Array.Empty<Citation>() };
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}


