using TiDB.Vector.Core;

namespace TiDB.Vector.Samples.Samples
{
    internal static class SearchSample
    {
        public static async Task RunAsync(TiDBVectorStore store)
        {
            var results = await store.SearchAsync("a swimming animal", topK: 3);
            Console.WriteLine($"Top {results.Count} results:");
            foreach (var r in results)
            {
                Console.WriteLine($"- {r.Id} | distance={r.Distance:0.0000} | {r.Content}");
            }
        }
    }
}


