using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;
using MySqlConnector;
using SemanticSlicer;
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
        private readonly bool _ensureSchema;
        private readonly bool _createVectorIndex;

        internal TiDBVectorStore(
            string connectionString,
            string defaultCollection,
            DistanceFunction distanceFunction,
            int embeddingDimension,
            IEmbeddingGenerator? embeddingGenerator,
            ITextGenerator? textGenerator,
            bool ensureSchema,
            bool createVectorIndex
        )
        {
            _connectionString = connectionString;
            _defaultCollection = defaultCollection;
            _distanceFunction = distanceFunction;
            _embeddingDimension = embeddingDimension;
            _embeddingGenerator = embeddingGenerator;
            _textGenerator = textGenerator;
            _ensureSchema = ensureSchema;
            _createVectorIndex = createVectorIndex;
        }

        public async Task EnsureSchemaAsync(
            bool? createVectorIndex = null,
            CancellationToken cancellationToken = default
        )
        {
            bool createIndex = createVectorIndex ?? _createVectorIndex;

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Create table tidb_vectors with composite PK (collection, id)
            // Create table with source and tags columns for efficient filtering
            string ddl =
                @"CREATE TABLE IF NOT EXISTS tidb_vectors (
                collection  VARCHAR(128) NOT NULL,
                id          VARCHAR(64) NOT NULL,
                content     TEXT NULL,
                metadata    JSON NULL,
                source      VARCHAR(512) NULL,
                tags        JSON NULL,
                embedding   VECTOR NOT NULL,
                created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                PRIMARY KEY (collection, id)
            );";

            await using (var cmd = new MySqlCommand(ddl, conn))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            if (createIndex)
            {
                // Best effort: ensure a TiFlash replica exists (ignored if not supported or already set)
                await EnsureTiFlashReplicaAsync(conn, cancellationToken).ConfigureAwait(false);
                await CreateVectorIndexAsync(conn, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task UpsertAsync(
            UpsertItem item,
            UpsertOptions? options = null,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(item);
            var collection = string.IsNullOrWhiteSpace(item.Collection)
                ? _defaultCollection
                : item.Collection;

            options ??= new UpsertOptions();

            // If chunking requested and no precomputed embedding provided, slice and upsert chunks
            if (
                options.UseChunking
                && (item.Embedding == null)
                && !string.IsNullOrEmpty(item.Content)
            )
            {
                if (_embeddingGenerator == null)
                    throw new InvalidOperationException(
                        "Chunking requires an IEmbeddingGenerator to be configured."
                    );

                var slicerOptions = BuildSlicerOptions(item.ContentType, options);
                var slicer = new Slicer(slicerOptions);
                var header = options.ChunkHeader ?? string.Empty;
                var chunks = slicer.GetDocumentChunks(item.Content!, null, header);

                // Batch embed all chunk contents
                var chunkContents = new List<string>(chunks.Count);
                foreach (var c in chunks)
                    chunkContents.Add(c.Content);
                var embeddings = await _embeddingGenerator
                    .GenerateBatchAsync(chunkContents, cancellationToken)
                    .ConfigureAwait(false);

                if (embeddings.Count != chunks.Count)
                    throw new InvalidOperationException(
                        "Embedding count did not match chunk count."
                    );

                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

                const string sqlChunk =
                    @"INSERT INTO tidb_vectors (collection, id, content, metadata, source, tags, embedding)
VALUES (@collection, @id, @content, @metadata, @source, @tags, CAST(@embeddingText AS VECTOR))
ON DUPLICATE KEY UPDATE
  content = VALUES(content),
  metadata = VALUES(metadata),
  source = VALUES(source),
  tags = VALUES(tags),
  embedding = VALUES(embedding),
  updated_at = CURRENT_TIMESTAMP;";

                for (int i = 0; i < chunks.Count; i++)
                {
                    var emb = embeddings[i];
                    if (_embeddingDimension > 0 && emb.Length != _embeddingDimension)
                        throw new InvalidOperationException(
                            $"Embedding dimension {emb.Length} does not match configured {_embeddingDimension}."
                        );
                    string embeddingTextChunk = VectorToText(emb);

                    string chunkId =
                        item.Id
                        + "/c"
                        + chunks[i]
                            .Index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string? content = chunks[i].Content;

                    // Merge metadata with chunk info
                    var metaNode = item.Metadata is null
                        ? new JsonObject()
                        : JsonNode.Parse(item.Metadata.RootElement.GetRawText()) as JsonObject
                            ?? new JsonObject();
                    metaNode["parentId"] = item.Id;
                    metaNode["chunkIndex"] = chunks[i].Index;
                    metaNode["contentType"] = item.ContentType.ToString();

                    string metadataJson = metaNode.ToJsonString();

                    await using var cmd = new MySqlCommand(sqlChunk, conn);
                    cmd.Parameters.AddWithValue("@collection", collection);
                    cmd.Parameters.AddWithValue("@id", chunkId);
                    cmd.Parameters.AddWithValue("@content", (object?)content ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@metadata", metadataJson);
                    cmd.Parameters.AddWithValue("@source", (object?)item.Source ?? DBNull.Value);
                    cmd.Parameters.AddWithValue(
                        "@tags",
                        item.Tags is null ? DBNull.Value : JsonSerializer.Serialize(item.Tags)
                    );
                    cmd.Parameters.AddWithValue("@embeddingText", embeddingTextChunk);
                    await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
                return;
            }
            else
            {
                // Single-row upsert (no chunking or embedding provided)
                float[] embedding = item.Embedding ?? [];
                if (embedding.Length == 0)
                {
                    if (_embeddingGenerator == null)
                        throw new InvalidOperationException(
                            "No embedding provided and no IEmbeddingGenerator configured."
                        );
                    embedding = await _embeddingGenerator
                        .GenerateAsync(item.Content ?? string.Empty, cancellationToken)
                        .ConfigureAwait(false);
                }
                if (_embeddingDimension > 0 && embedding.Length != _embeddingDimension)
                    throw new InvalidOperationException(
                        $"Embedding dimension {embedding.Length} does not match configured {_embeddingDimension}."
                    );

                string embeddingText = VectorToText(embedding);

                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

                const string sql =
                    @"INSERT INTO tidb_vectors (collection, id, content, metadata, source, tags, embedding)
VALUES (@collection, @id, @content, @metadata, @source, @tags, CAST(@embeddingText AS VECTOR))
ON DUPLICATE KEY UPDATE
  content = VALUES(content),
  metadata = VALUES(metadata),
  source = VALUES(source),
  tags = VALUES(tags),
  embedding = VALUES(embedding),
  updated_at = CURRENT_TIMESTAMP;";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@collection", collection);
                cmd.Parameters.AddWithValue("@id", item.Id);
                cmd.Parameters.AddWithValue("@content", (object?)item.Content ?? DBNull.Value);
                cmd.Parameters.AddWithValue(
                    "@metadata",
                    item.Metadata is null ? DBNull.Value : JsonSerializer.Serialize(item.Metadata)
                );
                cmd.Parameters.AddWithValue("@source", (object?)item.Source ?? DBNull.Value);
                cmd.Parameters.AddWithValue(
                    "@tags",
                    item.Tags is null ? DBNull.Value : JsonSerializer.Serialize(item.Tags)
                );
                cmd.Parameters.AddWithValue("@embeddingText", embeddingText);

                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task UpsertBatchAsync(
            IEnumerable<UpsertItem> items,
            UpsertOptions? options = null,
            CancellationToken cancellationToken = default
        )
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));
            var list = items as IList<UpsertItem> ?? [.. items];
            if (list.Count == 0)
                return;

            options ??= new UpsertOptions();

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var tx = await conn.BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);

            const string sql =
                @"INSERT INTO tidb_vectors (collection, id, content, metadata, source, tags, embedding)
VALUES (@collection, @id, @content, @metadata, @source, @tags, CAST(@embeddingText AS VECTOR))
ON DUPLICATE KEY UPDATE
  content = VALUES(content),
  metadata = VALUES(metadata),
  source = VALUES(source),
  tags = VALUES(tags),
  embedding = VALUES(embedding),
  updated_at = CURRENT_TIMESTAMP;";

            foreach (var item in list)
            {
                var collection = string.IsNullOrWhiteSpace(item.Collection)
                    ? _defaultCollection
                    : item.Collection;

                if (
                    options.UseChunking
                    && (item.Embedding == null)
                    && !string.IsNullOrEmpty(item.Content)
                )
                {
                    if (_embeddingGenerator == null)
                        throw new InvalidOperationException(
                            "Chunking requires an IEmbeddingGenerator to be configured."
                        );

                    var slicerOptions = BuildSlicerOptions(item.ContentType, options);
                    var slicer = new Slicer(slicerOptions);
                    var chunks = slicer.GetDocumentChunks(
                        item.Content!,
                        null,
                        options.ChunkHeader ?? string.Empty
                    );

                    var chunkContents = new List<string>(chunks.Count);
                    foreach (var c in chunks)
                        chunkContents.Add(c.Content);
                    var embeddings = await _embeddingGenerator
                        .GenerateBatchAsync(chunkContents, cancellationToken)
                        .ConfigureAwait(false);
                    if (embeddings.Count != chunks.Count)
                        throw new InvalidOperationException(
                            "Embedding count did not match chunk count."
                        );

                    for (int i = 0; i < chunks.Count; i++)
                    {
                        var emb = embeddings[i];
                        if (_embeddingDimension > 0 && emb.Length != _embeddingDimension)
                            throw new InvalidOperationException(
                                $"Embedding dimension {emb.Length} does not match configured {_embeddingDimension}."
                            );

                        string embeddingText = VectorToText(emb);
                        string chunkId =
                            item.Id
                            + "/c"
                            + chunks[i]
                                .Index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string? content = chunks[i].Content;

                        var metaNode = item.Metadata is null
                            ? new JsonObject()
                            : JsonNode.Parse(item.Metadata.RootElement.GetRawText()) as JsonObject
                                ?? new JsonObject();
                        metaNode["parentId"] = item.Id;
                        metaNode["chunkIndex"] = chunks[i].Index;
                        metaNode["contentType"] = item.ContentType.ToString();
                        string metadataJson = metaNode.ToJsonString();

                        await using var cmd = new MySqlCommand(sql, conn, tx);
                        cmd.Parameters.AddWithValue("@collection", collection);
                        cmd.Parameters.AddWithValue("@id", chunkId);
                        cmd.Parameters.AddWithValue("@content", (object?)content ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@metadata", metadataJson);
                        cmd.Parameters.AddWithValue("@embeddingText", embeddingText);
                        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    float[] embedding = item.Embedding ?? [];
                    if (embedding.Length == 0)
                    {
                        if (_embeddingGenerator == null)
                            throw new InvalidOperationException(
                                "No embedding provided and no IEmbeddingGenerator configured."
                            );
                        embedding = await _embeddingGenerator
                            .GenerateAsync(item.Content ?? string.Empty, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    if (_embeddingDimension > 0 && embedding.Length != _embeddingDimension)
                        throw new InvalidOperationException(
                            $"Embedding dimension {embedding.Length} does not match configured {_embeddingDimension}."
                        );

                    string embeddingText = VectorToText(embedding);
                    await using var cmd = new MySqlCommand(sql, conn, tx);
                    cmd.Parameters.AddWithValue("@collection", collection);
                    cmd.Parameters.AddWithValue("@id", item.Id);
                    cmd.Parameters.AddWithValue("@content", (object?)item.Content ?? DBNull.Value);
                    cmd.Parameters.AddWithValue(
                        "@metadata",
                        item.Metadata is null
                            ? DBNull.Value
                            : JsonSerializer.Serialize(item.Metadata)
                    );
                    cmd.Parameters.AddWithValue("@source", (object?)item.Source ?? DBNull.Value);
                    cmd.Parameters.AddWithValue(
                        "@tags",
                        item.Tags is null ? DBNull.Value : JsonSerializer.Serialize(item.Tags)
                    );
                    cmd.Parameters.AddWithValue("@embeddingText", embeddingText);
                    await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<SearchResult>> SearchAsync(
            string query,
            int topK = 5,
            SearchOptions? searchOptions = null,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(query))
                return Array.Empty<SearchResult>();
            if (_embeddingGenerator == null)
                throw new InvalidOperationException(
                    "Search requires an IEmbeddingGenerator to embed the query."
                );

            var queryVec = await _embeddingGenerator
                .GenerateAsync(query, cancellationToken)
                .ConfigureAwait(false);
            if (_embeddingDimension > 0 && queryVec.Length != _embeddingDimension)
                throw new InvalidOperationException(
                    $"Query embedding dimension {queryVec.Length} does not match configured {_embeddingDimension}."
                );
            string queryText = VectorToText(queryVec);

            string distanceExpr =
                _distanceFunction == DistanceFunction.Cosine
                    ? "VEC_COSINE_DISTANCE(embedding, @queryVec)"
                    : "VEC_L2_DISTANCE(embedding, @queryVec)";

            // Build filtering conditions
            var whereConditions = new List<string>();
            var parameters = new List<(string name, object value)>();

            // Collection filtering (single collection only)
            if (!string.IsNullOrWhiteSpace(searchOptions?.Collection))
            {
                whereConditions.Add("collection = @collection");
                parameters.Add(("@collection", searchOptions.Collection));
            }
            else
            {
                whereConditions.Add("collection = @collection");
                parameters.Add(("@collection", _defaultCollection));
            }

            // Tag filtering with key-value pairs using dedicated tags JSON column
            if (searchOptions?.TagFilters != null && searchOptions.TagFilters.Count > 0)
            {
                var tagConditions = new List<string>();
                foreach (var (key, value) in searchOptions.TagFilters)
                {
                    var paramName = $"@tag_{key}";
                    tagConditions.Add($"JSON_UNQUOTE(JSON_EXTRACT(tags, '$.{key}')) = {paramName}");
                    parameters.Add((paramName, value));
                }

                if (tagConditions.Count > 0)
                {
                    var tagOperator = searchOptions.TagMode == TagFilterMode.And ? " AND " : " OR ";
                    whereConditions.Add($"({string.Join(tagOperator, tagConditions)})");
                }
            }

            // To leverage ANN index, perform KNN first, then filter in outer query.
            int kPrime = Math.Max(topK * 3, topK + 20);
            string whereClause = whereConditions.Count > 0 ? $"WHERE {string.Join(" AND ", whereConditions)}" : "";

            string sql =
                $@"SELECT * FROM (
  SELECT id, collection, content, metadata, source, tags, {distanceExpr} AS distance
  FROM tidb_vectors
  {whereClause}
  ORDER BY distance
  LIMIT @kPrime
) t
ORDER BY distance
LIMIT @k";

            var results = new List<SearchResult>(topK);
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = new MySqlCommand(sql, conn);

            // Add dynamic parameters
            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }

            cmd.Parameters.AddWithValue("@queryVec", queryText);
            cmd.Parameters.AddWithValue("@kPrime", kPrime);
            cmd.Parameters.AddWithValue("@k", topK);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var id = reader.GetString(reader.GetOrdinal("id"));
                var collection = reader.GetString(reader.GetOrdinal("collection"));
                string? content = reader.IsDBNull(reader.GetOrdinal("content"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("content"));
                JsonDocument? metadata = null;
                if (!reader.IsDBNull(reader.GetOrdinal("metadata")))
                {
                    var json = reader.GetString(reader.GetOrdinal("metadata"));
                    metadata = JsonDocument.Parse(json);
                }
                string? source = reader.IsDBNull(reader.GetOrdinal("source"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("source"));
                JsonDocument? tags = null;
                if (!reader.IsDBNull(reader.GetOrdinal("tags")))
                {
                    var tagsJson = reader.GetString(reader.GetOrdinal("tags"));
                    tags = JsonDocument.Parse(tagsJson);
                }
                double distance = reader.GetDouble(reader.GetOrdinal("distance"));

                results.Add(
                    new SearchResult
                    {
                        Id = id,
                        Collection = collection,
                        Content = content,
                        Metadata = metadata,
                        Source = source,
                        Tags = tags,
                        Distance = distance,
                    }
                );
            }

            return results;
        }

        public async Task CompactAsync(CancellationToken cancellationToken = default)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            const string sql = "ALTER TABLE tidb_vectors COMPACT;";
            try
            {
                await using var cmd = new MySqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (MySqlException)
            {
                // Best-effort; ignore if unsupported or lacks privileges
            }
        }

        public async Task<bool> IsVectorIndexUsedAsync(
            string testQuery,
            int topK = 5,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(testQuery))
                return false;

            // Build an EXPLAIN for our standard KNN query to look for 'annIndex:' in operator info
            var queryVec =
                _embeddingGenerator == null
                    ? new float[_embeddingDimension]
                    : await _embeddingGenerator
                        .GenerateAsync(testQuery, cancellationToken)
                        .ConfigureAwait(false);

            if (queryVec.Length != _embeddingDimension)
                Array.Resize(ref queryVec, _embeddingDimension);
            string queryText = VectorToText(queryVec);

            string distanceExpr =
                _distanceFunction == DistanceFunction.Cosine
                    ? "VEC_COSINE_DISTANCE(embedding, @queryVec)"
                    : "VEC_L2_DISTANCE(embedding, @queryVec)";

            int kPrime = Math.Max(topK * 3, topK + 20);
            string explain =
                $@"EXPLAIN SELECT * FROM (
  SELECT id, collection, content, metadata, {distanceExpr} AS distance
  FROM tidb_vectors
  ORDER BY distance
  LIMIT @kPrime
) t
WHERE collection = @collection
ORDER BY distance
LIMIT @k";

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = new MySqlCommand(explain, conn);
            cmd.Parameters.AddWithValue("@collection", _defaultCollection);
            cmd.Parameters.AddWithValue("@queryVec", queryText);
            cmd.Parameters.AddWithValue("@kPrime", kPrime);
            cmd.Parameters.AddWithValue("@k", topK);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (!reader.IsDBNull(i))
                    {
                        var val = reader.GetValue(i)?.ToString();
                        if (
                            !string.IsNullOrEmpty(val)
                            && val.IndexOf("annIndex:", StringComparison.OrdinalIgnoreCase) >= 0
                        )
                            return true;
                    }
                }
            }

            return false;
        }

        public async Task<Answer> AskAsync(
            string query,
            int topK = 6,
            SearchOptions? searchOptions = null,
            object? answerOptions = null,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(query))
                return new Answer { Text = string.Empty, Sources = Array.Empty<Citation>() };
            if (_textGenerator == null)
                throw new InvalidOperationException(
                    "Ask requires an ITextGenerator to be configured."
                );

            var hits = await SearchAsync(query, topK, searchOptions, cancellationToken)
                .ConfigureAwait(false);

            var sb = new StringBuilder();
            sb.AppendLine(
                "You are a helpful assistant. Use the provided context to answer the user's question. If the answer isn't in the context, say you don't know. Keep answers concise."
            );
            string system = sb.ToString();

            var contextBuilder = new StringBuilder();
            for (int i = 0; i < hits.Count; i++)
            {
                var h = hits[i];
                contextBuilder.AppendLine($"[SourceId: {h.Id}] (distance={h.Distance:0.0000})");
                if (!string.IsNullOrEmpty(h.Content))
                {
                    // Limit content length to avoid exceeding token limits in examples
                    var content =
                        h.Content.Length > 1500 ? h.Content.Substring(0, 1500) + "..." : h.Content;
                    contextBuilder.AppendLine(content);
                }
                contextBuilder.AppendLine();
            }

            var messages = new List<(string role, string content)>
            {
                ("user", $"CONTEXT:\n{contextBuilder}\nQUESTION: {query}"),
            };

            string answerText = await _textGenerator
                .CompleteAsync(system, messages, cancellationToken)
                .ConfigureAwait(false);

            var citations = new List<Citation>(hits.Count);
            foreach (var h in hits)
            {
                citations.Add(
                    new Citation
                    {
                        Id = h.Id,
                        Snippet = h.Content,
                        Distance = h.Distance,
                        Source = h.Source,
                    }
                );
            }

            return new Answer { Text = answerText, Sources = citations };
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        private static string VectorToText(IReadOnlyList<float> vector)
        {
            // Produce normalized format like: [0.3,0.5,-0.1]
            var sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < vector.Count; i++)
            {
                if (i > 0)
                    sb.Append(',');
                // invariant culture to avoid locale decimal commas
                sb.Append(vector[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            sb.Append(']');
            return sb.ToString();
        }

        private async Task EnsureTiFlashReplicaAsync(
            MySqlConnection conn,
            CancellationToken cancellationToken
        )
        {
            const string sql = "ALTER TABLE tidb_vectors SET TIFLASH REPLICA 1;";
            try
            {
                await using var cmd = new MySqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (MySqlException)
            {
                // Ignore if not supported or already configured
            }
        }

        private async Task CreateVectorIndexAsync(
            MySqlConnection conn,
            CancellationToken cancellationToken
        )
        {
            string indexName =
                _distanceFunction == DistanceFunction.Cosine
                    ? "idx_tidb_vectors_embedding_cosine"
                    : "idx_tidb_vectors_embedding_l2";

            string createSql =
                _distanceFunction == DistanceFunction.Cosine
                    ? $"CREATE VECTOR INDEX {indexName} ON tidb_vectors ((VEC_COSINE_DISTANCE(embedding))) USING HNSW;"
                    : $"CREATE VECTOR INDEX {indexName} ON tidb_vectors ((VEC_L2_DISTANCE(embedding))) USING HNSW;";

            // Pre-check via INFORMATION_SCHEMA.TIFLASH_INDEXES if possible
            try
            {
                const string existsSql =
                    @"SELECT COUNT(1) FROM INFORMATION_SCHEMA.TIFLASH_INDEXES
WHERE TIDB_DATABASE = DATABASE() AND TIDB_TABLE = 'tidb_vectors' AND INDEX_NAME = @indexName;";
                await using var existsCmd = new MySqlCommand(existsSql, conn);
                existsCmd.Parameters.AddWithValue("@indexName", indexName);
                var countObj = await existsCmd
                    .ExecuteScalarAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (countObj != null && Convert.ToInt64(countObj) > 0)
                {
                    return; // index already exists
                }
            }
            catch (MySqlException)
            {
                // Best effort; continue to attempt creating the index
            }

            try
            {
                await using var cmd = new MySqlCommand(createSql, conn);
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (MySqlException ex)
            {
                // Ignore if index already exists (message variants)
                var msg = ex.Message ?? string.Empty;
                var lower = msg.ToLowerInvariant();
                if (
                    lower.Contains("already exist")
                    || (lower.Contains("duplicate") && lower.Contains("index"))
                )
                {
                    return;
                }
                throw;
            }
        }

        private static SlicerOptions BuildSlicerOptions(
            ContentType contentType,
            UpsertOptions options
        )
        {
            var separators = contentType switch
            {
                ContentType.Markdown => Separators.Markdown,
                ContentType.Html => Separators.Html,
                _ => Separators.Text,
            };

            var slicerOptions = new SlicerOptions
            {
                MaxChunkTokenCount = Math.Max(100, options.MaxTokensPerChunk),
                Separators = separators,
                StripHtml = contentType == ContentType.Html && options.StripHtml,
            };
            return slicerOptions;
        }
    }
}
