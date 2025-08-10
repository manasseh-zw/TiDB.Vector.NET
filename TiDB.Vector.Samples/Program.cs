using System.Text.Json;
using TiDB.Vector.Core;
using TiDB.Vector.Models;
using TiDB.Vector.OpenAI.Builder;
using TiDB.Vector.Samples.Samples;

// Load from .env via DotEnv.Net
TiDB.Vector.Samples.AppConfig.Load();
var apiKey = TiDB.Vector.Samples.AppConfig.OpenAIApiKey;
var connString = TiDB.Vector.Samples.AppConfig.TiDBConnectionString;

var store = new TiDBVectorStoreBuilder(connString)
    .WithDefaultCollection("docs")
    .WithDistanceFunction(DistanceFunction.Cosine)
    .AddOpenAITextEmbedding(apiKey: apiKey, model: "text-embedding-3-small", dimension: 1536)
    .AddOpenAIChatCompletion(apiKey: apiKey, model: "gpt-4o-mini")
    .EnsureSchema(createVectorIndex: true)
    .Build();

await store.EnsureSchemaAsync();
await UpsertSample.RunAsync(store);
await SearchSample.RunAsync(store);

// Optional: check if ANN index is used
var idxUsed = await store.IsVectorIndexUsedAsync("a swimming animal", topK: 3);
Console.WriteLine($"Vector index used: {idxUsed}");

await AskSample.RunAsync(store);