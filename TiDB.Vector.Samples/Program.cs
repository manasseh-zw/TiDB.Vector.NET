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
    Metadata = JsonDocument.Parse("{\"source\":\"sample\"}"),
    // Provide an explicit embedding to avoid needing an embedding generator in this sample
    Embedding = new float[1568]
});
Console.WriteLine("Schema ensured and sample row upserted.");