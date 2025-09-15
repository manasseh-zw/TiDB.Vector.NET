using TiDB.Vector.Core;
using TiDB.Vector.Models;
using TiDB.Vector.OpenAI.Builder;

namespace TiDB.Vector.Samples;

public static class TableNameSample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("🧪 TableName + EnsureSchema sample");
        Console.WriteLine("=================================");

        var tableName = "tidb_vectors_sdk_test"; // customize as needed

        var store = new TiDBVectorStoreBuilder(AppConfig.TiDBConnectionString)
            .WithTableName(tableName)
            .EnsureSchema(createVectorIndex: true) // enable lazy schema creation on first use
            .AddOpenAI(AppConfig.OpenAIApiKey)
            .AddOpenAITextEmbedding("text-embedding-3-small", 1536)
            .Build();

        // No explicit EnsureSchemaAsync call here to validate lazy creation on first upsert

        var item = new UpsertItem
        {
            Id = "hello-1",
            Collection = "samples",
            Content = "Hello TiDB Vector!",
            Tags = new[] { new Tag("Env", "Test"), new Tag("Sample", "TableName") },
        };

        Console.WriteLine($"➡️ Upserting into table '{tableName}'...");
        await store.UpsertAsync(item);
        Console.WriteLine("✅ Upsert completed");

        Console.WriteLine("➡️ Running a quick search...");
        var results = await store.SearchAsync("Hello", topK: 3);
        Console.WriteLine($"✅ Got {results.Count} result(s)");
        foreach (var r in results)
        {
            Console.WriteLine($"   📄 {r.Id} | {r.Collection} | dist={r.Distance:F4}");
        }

        Console.WriteLine("🏁 TableName + EnsureSchema sample done\n");
    }
}
