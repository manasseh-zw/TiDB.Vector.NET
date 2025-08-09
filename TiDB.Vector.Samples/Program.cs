using System.Text.Json;
using TiDB.Vector.Core;
using TiDB.Vector.Models;

var store = new TiDBVectorStoreBuilder("Server=localhost;Port=4000;User ID=root;Password=;Database=test;")
    .WithDefaultCollection("docs")
    .WithDistanceFunction(DistanceFunction.Cosine)
    .EnsureSchema(createVectorIndex: false)
    .Build();

await store.EnsureSchemaAsync();

await store.UpsertAsync(new UpsertItem
{
    Id = "sample-1",
    Collection = "docs",
    Content = "Hello TiDB Vector",
    Metadata = JsonDocument.Parse("{\"source\":\"sample\"}")
});

var results = await store.SearchAsync("hello", topK: 3);
Console.WriteLine($"Search results: {results.Count}");