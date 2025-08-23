## TiDB.Vector.NET

Ergonomic C# SDK for TiDB Vector Search: upsert, search, and RAG with a fluent builder, OpenAI embeddings/chat integration, and DX-first defaults.

Repository: [manasseh-zw/TiDB.Vector.NET](https://github.com/manasseh-zw/TiDB.Vector.NET)

### Features

- Fluent builder to wire connection, embeddings, and chat
- Default schema with fixed-dimension `VECTOR(D)` and optional HNSW index
- Safe, parameterized SQL via `MySqlConnector`
- OpenAI providers in a separate package (`TiDB.Vector.OpenAI`)
- Simple RAG helper (`AskAsync`) that cites sources
- Samples with `.env` support via `dotenv.net`

### Projects

- `TiDB.Vector` (Core): store API, schema, SQL
- `TiDB.Vector.OpenAI`: official OpenAI .NET SDK providers (embeddings/chat)
- `TiDB.Vector.AzureOpenAI`: Azure OpenAI providers (embeddings/chat)
- `TiDB.Vector.Samples`: runnable examples (upsert, search, ask)

### Requirements

- .NET 8.0+
- TiDB v8.4+ (v8.5+ recommended); TiDB Cloud supported
- For HNSW index: TiFlash replica required on the target table

### Quickstart (local dev)

1) Clone the repo:

```bash
git clone https://github.com/manasseh-zw/TiDB.Vector.NET
cd TiDB.Vector.NET
```

2) Create a `.env` file in `TiDB.Vector.Samples/`:

```
# Required for all samples
TIDB_CONN_STRING=Server=<host>;Port=4000;User ID=<user>;Password=<pwd>;Database=<db>;SslMode=VerifyFull;

# For OpenAI samples
OPENAI_API_KEY=sk-...

# For Azure OpenAI samples
AZURE_OPENAI_API_KEY=your-azure-key
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_EMBEDDING_DEPLOYMENT=your-embedding-deployment
AZURE_OPENAI_CHAT_DEPLOYMENT=your-chat-deployment
```

3) Run samples:

```bash
dotnet run --project TiDB.Vector.Samples
```

The sample will:
- Ensure the default schema and (optionally) HNSW index
- Upsert a couple of documents
- Perform a vector search
- Call `AskAsync` to answer a question using retrieved context

### Core API (glance)

```csharp
using TiDB.Vector.Core;
using TiDB.Vector.OpenAI.Builder;

var store = new TiDBVectorStoreBuilder(
        Environment.GetEnvironmentVariable("TIDB_CONN_STRING")!)
    .WithDefaultCollection("docs")
    .WithDistanceFunction(DistanceFunction.Cosine)
    .AddOpenAITextEmbedding(
        apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
        model: "text-embedding-3-small",
        dimension: 1536)
    .AddOpenAIChatCompletion(
        apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
        model: "gpt-4o-mini")
    .EnsureSchema(createVectorIndex: true)
    .Build();

await store.EnsureSchemaAsync();

await store.UpsertAsync(new UpsertItem
{
    Id = "sample-1",
    Collection = "docs",
    Content = "Fish live in water and are known for their swimming abilities."
});

var results = await store.SearchAsync("a swimming animal", topK: 3);
var answer = await store.AskAsync("Name an animal that swims.");
```

#### Azure OpenAI Example

```csharp
using TiDB.Vector.Core;
using TiDB.Vector.AzureOpenAI.Builder;

var store = new TiDBVectorStoreBuilder(
        Environment.GetEnvironmentVariable("TIDB_CONN_STRING")!)
    .WithDefaultCollection("docs")
    .WithDistanceFunction(DistanceFunction.Cosine)
    .AddAzureOpenAITextEmbedding(
        apiKey: Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!,
        endpoint: Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,
        deploymentName: Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT")!,
        dimension: 1536)
    .AddAzureOpenAIChatCompletion(
        apiKey: Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!,
        endpoint: Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,
        deploymentName: Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT")!)
    .EnsureSchema(createVectorIndex: true)
    .Build();

### Default Schema and Index

- Table: `tidb_vectors`
  - `collection VARCHAR(128)` + `id VARCHAR(64)` as composite primary key
  - `embedding VECTOR(D)` where `D` = embedding dimension (e.g., 1536)
  - `content TEXT`, `metadata JSON`, timestamps
- Index (optional, HNSW):
  - Cosine: `CREATE VECTOR INDEX idx_tidb_vectors_embedding_cosine ON tidb_vectors ((VEC_COSINE_DISTANCE(embedding))) USING HNSW;`
  - L2: `CREATE VECTOR INDEX idx_tidb_vectors_embedding_l2 ON tidb_vectors ((VEC_L2_DISTANCE(embedding))) USING HNSW;`

Notes:
- Fixed dimension is required to build the vector index.
- TiFlash replica is required for building/using the HNSW index.
- To keep index usage when filtering, the SDK performs KNN first in a subquery, then applies filters.

### OpenAI integration

Provided via `TiDB.Vector.OpenAI` using the official OpenAI .NET SDK 2.x:
- Embeddings: `EmbeddingClient` (`text-embedding-3-small` 1536 dims, `text-embedding-3-large` 3072 dims)
- Chat: `ChatClient` (e.g., `gpt-4o-mini`)

Ensure the table dimension matches the chosen embedding model’s dimension.

### Connection string (TiDB Cloud)

Use standard ADO.NET format (not URL form), e.g.:

```
Server=<host>;Port=4000;User ID=<user>;Password=<pwd>;Database=<db>;SslMode=VerifyFull;
```

If you must provide a custom CA bundle, append:

```
SslCa=C:\\path\\to\\isrgrootx1.pem;
```

### Roadmap (high level)

- Iteration 1: Skeleton and builder
- Iteration 2: Schema management + upsert/search
- Iteration 3: Vector index + TiFlash helpers
- Iteration 4: Chunking (out-of-the-box text splitter)
- Iteration 5: OpenAI embeddings/chat + RAG `AskAsync`
- Iteration 6: Azure OpenAI integration + additional providers
- Iteration 7+: Filters, metrics, streaming, more providers

### Contributing

We welcome issues and PRs! To contribute:
- Fork the repo and create a feature branch
- Follow the existing code style (explicit naming, clear control flow)
- Keep public APIs strongly-typed and documented
- Add/update samples if you change user-facing behavior
- Run `dotnet build` before submitting your PR

Open an issue to discuss larger proposals or provider integrations. Repo: [manasseh-zw/TiDB.Vector.NET](https://github.com/manasseh-zw/TiDB.Vector.NET)

### Acknowledgements

- TiDB Vector Search (data types, functions, HNSW index)
- Official OpenAI .NET SDK for embeddings/chat


