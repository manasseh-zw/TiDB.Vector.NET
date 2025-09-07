## TiDB.Vector.NET – DX-first Implementation Plan

### Vision and Goals

- **Primary goal**: Provide an ergonomic C# SDK to upsert and search vectors in TiDB using a fluent builder, with first-class OpenAI embeddings and chat, and optional chunking.
- **DX principles**: fluent configuration, sensible defaults, pluggable providers, safe SQL, good error messages, and minimal boilerplate to get started.

### Target Developer Experience

```csharp
var store = new TiDBVectorStoreBuilder("Server=...;Database=...;User=...;Password=...")
    .AddOpenAI(apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
    .AddOpenAITextEmbedding(model: "text-embedding-3-large", dimension: 3072)
    .AddOpenAIChatCompletion(model: "gpt-4o-mini")
    .UseSemanticSlicer(options =>
    {
        options.TargetTokens = 600;
        options.OverlapTokens = 80;
    })
    .WithDefaultCollection("docs")
    .WithDistanceFunction(DistanceFunction.Cosine)
    .EnsureSchema(createVectorIndex: true)
    .Build();

await store.UpsertAsync(new UpsertItem
{
    Id = "doc-1",
    Collection = "docs",
    Content = "Long text...",
    Metadata = JsonDocument.Parse("{\"source\":\"manual\"}")
}, new UpsertOptions { UseChunking = true });

var hits = await store.SearchAsync("swimming animal", topK: 5);
var answer = await store.AskAsync("What animals can swim?", topK: 6);
```

### Architecture Overview

- **Core**: `TiDBVectorStore` orchestrates embedding generation, SQL upserts, KNN search, post-filtering, and optional RAG.
- **Abstractions**: `IEmbeddingGenerator`, `ITextGenerator`, `IChunker` to allow custom providers.
- **Providers**: Built-in OpenAI embedding and chat; adapter for `drittich.SemanticSlicer`.
- **Storage**: One default table `vectors` with fixed `VECTOR(D)`; optional HNSW index using `VEC_COSINE_DISTANCE` or `VEC_L2_DISTANCE`.
- **Safety**: MySQL parameterization, connection pooling (`MySqlConnector`), prepared statements for hot paths.

### Public API Surface

```csharp
public sealed class TiDBVectorStoreBuilder { /* fluent config + Build() */ }
public sealed class TiDBVectorStore : IAsyncDisposable
{
    Task EnsureSchemaAsync(bool createVectorIndex = true, CancellationToken ct = default);
    Task UpsertAsync(UpsertItem item, UpsertOptions? options = null, CancellationToken ct = default);
    Task UpsertBatchAsync(IEnumerable<UpsertItem> items, UpsertOptions? options = null, CancellationToken ct = default);
    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int topK = 5, SearchOptions? options = null, CancellationToken ct = default);
    Task<Answer> AskAsync(string query, int topK = 6, AskOptions? options = null, CancellationToken ct = default);
}

public interface IEmbeddingGenerator
{
    int Dimension { get; }
    Task<float[]> GenerateAsync(string text, CancellationToken ct = default);
    Task<IReadOnlyList<float[]>> GenerateBatchAsync(IEnumerable<string> texts, CancellationToken ct = default);
}

public interface ITextGenerator
{
    Task<string> CompleteAsync(
        string system,
        IReadOnlyList<(string role, string content)> messages,
        CancellationToken ct = default);
}

public interface IChunker
{
    IReadOnlyList<Chunk> Chunk(string text, ChunkingOptions options);
}
```

### Default Data Model and DDL

- **Table**: `vectors` (one-table-by-default; multi-collection via `collection` column)

```sql
CREATE TABLE IF NOT EXISTS vectors (
    id          VARCHAR(64) PRIMARY KEY,
    collection  VARCHAR(128) NOT NULL,
    content     TEXT NULL,
    metadata    JSON NULL,
    embedding   VECTOR(@D) NOT NULL,
    created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    KEY idx_collection (collection)
);
```

- **Vector index (optional, HNSW)**

```sql
-- Cosine
CREATE VECTOR INDEX IF NOT EXISTS idx_vectors_embedding
ON vectors ((VEC_COSINE_DISTANCE(embedding))) USING HNSW;

-- Or L2
CREATE VECTOR INDEX IF NOT EXISTS idx_vectors_embedding
ON vectors ((VEC_L2_DISTANCE(embedding))) USING HNSW;
```

Notes

- Index requires fixed dimension `VECTOR(D)`. `D` is derived from the configured embedding generator.
- For filtered queries, we perform KNN first, then apply filters to preserve ANN index usage.

### SQL Patterns

- **KNN search (Cosine example)**

```sql
SELECT id, collection, content, metadata,
       VEC_COSINE_DISTANCE(embedding, @queryVec) AS distance
FROM vectors
WHERE collection = @collection
ORDER BY distance
LIMIT @k;
```

- **KNN with filters (post-filter)**

```sql
SELECT * FROM (
  SELECT id, collection, content, metadata,
         VEC_COSINE_DISTANCE(embedding, @queryVec) AS distance
  FROM vectors
  WHERE collection = @collection
  ORDER BY distance
  LIMIT @kPrime
) t
WHERE JSON_EXTRACT(t.metadata, '$.source') = 'manual'
ORDER BY distance
LIMIT @k;
```

- **Upsert**

```sql
INSERT INTO vectors (id, collection, content, metadata, embedding)
VALUES (@id, @collection, @content, @metadata, CAST(@embeddingText AS VECTOR))
ON DUPLICATE KEY UPDATE
  content = VALUES(content),
  metadata = VALUES(metadata),
  embedding = VALUES(embedding),
  updated_at = CURRENT_TIMESTAMP;
```

### Models and Options

```csharp
public sealed record UpsertItem
{
    string Id { get; init; } = default!;
    string Collection { get; init; } = default!;
    string? Content { get; init; }
    JsonDocument? Metadata { get; init; }
    float[]? Embedding { get; init; } // optional; generated if null
}

public sealed record UpsertOptions
{
    bool UseChunking { get; init; } = false;
    bool Overwrite { get; init; } = true;
}

public sealed record SearchResult
{
    string Id { get; init; } = default!;
    string Collection { get; init; } = default!;
    string? Content { get; init; }
    JsonDocument? Metadata { get; init; }
    double Distance { get; init; }
}

public sealed record Answer
{
    string Text { get; init; } = default!;
    IReadOnlyList<Citation> Sources { get; init; } = Array.Empty<Citation>();
}

public sealed record Citation
{
    string Id { get; init; } = default!;
    string? Snippet { get; init; }
    double Distance { get; init; }
}
```

### Providers

- **Embeddings**: Built-in OpenAI client; implements `IEmbeddingGenerator` with batch support and backoff. Supports `text-embedding-3-small/large` and dimension override when available.
- **Chat**: Built-in OpenAI chat completion for `AskAsync` prompt orchestration.
- **Chunking**: Adapter for `drittich.SemanticSlicer` as `IChunker` with sensible defaults and overridable options.

### Dependencies

- `MySqlConnector` (ADO.NET, async, pooling)
- `System.Text.Json`
- OpenAI HTTP client (lightweight) to `api.openai.com`
- `drittich.SemanticSlicer` (via adapter)

### Error Handling and Safety

- Parameterized queries; no string concatenation for SQL values.
- Validate embedding dimension matches `VECTOR(D)`.
- Clear exceptions for common misconfigurations (missing index vs. filters, wrong dimension, null API key).
- Cancellation tokens, timeouts, and simple retry for transient errors.

### Iterations and Deliverables

1. Skeleton and Builder

   - Abstractions, models, builder, empty `TiDBVectorStore`.
   - Samples compile and run (no-op logic returning placeholders).

2. Schema Management + Basic Upsert/Search

   - `EnsureSchemaAsync()` creates table with `VECTOR(D)`.
   - `UpsertAsync/BatchAsync` with embedding generation if missing.
   - `SearchAsync` KNN (no filters).
   - Sample: connect, ensure schema, upsert few docs, search.

3. Vector Index (HNSW)

   - Create index for Cosine/L2 per configuration.
   - Helpers to `EXPLAIN` and report whether ANN index is used.
   - Docs on pre-filter limitation and subquery pattern.

4. Chunking Support

   - Integrate SemanticSlicer; store chunk rows (optional `parent_id`, offsets).
   - `UseChunking` in upsert; retrieval returns chunk-level results.

5. Ask (RAG)

   - Retrieve top-k chunks; assemble prompt with citations; call `ITextGenerator`.
   - Return `Answer` with `Sources`; optional streaming later.

6. DX Polish

   - Filter support, collections, metadata predicates (post-filter pattern built-in).
   - Concurrency controls, batching, rate-limits, retries.
   - Logging hooks (ILogger), diagnostics IDs.
   - README quickstart and API docs.

7. Performance

   - Prepared statements, batch insert optimizations.
   - Parallel embedding with backpressure.
   - Optional forced compaction helper (`ALTER TABLE ... COMPACT`).

8. Tests
   - Unit tests for SQL builders and serialization.
   - Integration tests gated by env variables; optional Docker/TiDB.

### Testing Strategy

- Unit: input validation, SQL generation, mapping, and distance selection.
- Integration: live TiDB where available; otherwise skip with clear message.
- Smoke: sample app e2e (ensure schema, upsert, search, ask stub).

### Performance Considerations

- Use `ORDER BY VEC_*_DISTANCE(...) LIMIT K` to trigger ANN index when present.
- Avoid pre-filters; perform post-filter via subquery with `k' >= k`.
- Batch inserts, prepared statements, and pooled connections.

### Security and Config

- No secrets in code; accept API keys via environment or builder.
- Optional TLS params passed via connection string.

### Completed Advanced Features ✅

**Iteration 8: Advanced Filtering and Source Tracking (COMPLETED)**

- **Collection Filtering**: Filter by single collection for simplified, predictable results
- **Tag-based Filtering**: Key-value filtering using dedicated JSON tags column
- **Source Tracking**: Track document origins (URLs, file paths, etc.) in dedicated column
- **Dedicated Tags Column**: Efficient JSON-based filtering without parsing metadata
- **Multi-tenant Support**: Organization/department filtering for SaaS applications
- **Performance Optimized**: Leverages TiDB's binary JSON serialization for quick access
- **Index-friendly**: Maintains vector index usage during filtered searches

**API Examples:**

```csharp
// Collection filtering
var results = await store.SearchAsync("query",
    searchOptions: new SearchOptions { Collection = "engineering-docs" });

// Tag filtering
var results = await store.SearchAsync("query",
    searchOptions: new SearchOptions
    {
        TagFilters = new Dictionary<string, string>
        {
            ["OrganizationId"] = "org-123",
            ["Department"] = "Engineering"
        }
    });

// Combined filtering
var results = await store.SearchAsync("query",
    searchOptions: new SearchOptions
    {
        Collection = "engineering-docs",
        TagFilters = new Dictionary<string, string>
        {
            ["OrganizationId"] = "org-123"
        }
    });
```

### Future Extensions

- Additional embedding providers (Google Gemini, Anthropic Claude, Text Embeddings Inference, HuggingFace Inference API).
- Multi-table collections; per-collection distance function and dimension.
- Hybrid search (BM25 + vector) and reranking.
- Streaming answers and function/tool calling.

### Alignment with TiDB Vector Capabilities

- Uses `VECTOR(D)` and vector distance functions: `VEC_COSINE_DISTANCE`, `VEC_L2_DISTANCE`.
- HNSW vector index via `CREATE VECTOR INDEX ... USING HNSW`.
- Honors index restrictions: single vector column, fixed dimension, ASC order, matching distance function.
