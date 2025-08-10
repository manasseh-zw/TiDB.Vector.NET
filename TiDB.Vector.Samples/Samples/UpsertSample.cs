using System.Text.Json;
using TiDB.Vector.Core;
using TiDB.Vector.Models;

namespace TiDB.Vector.Samples.Samples
{
    internal static class UpsertSample
    {
        public static async Task RunAsync(TiDBVectorStore store)
        {
            var docs = new[]
            {
                new UpsertItem
                {
                    Id = "sample-1",
                    Collection = "docs",
                    Content = "Cristiano Ronaldo is a Portuguese professional footballer widely regarded as one of the greatest players of all time.",
                    Metadata = JsonDocument.Parse("{\"source\":\"sample\",\"topic\":\"sports\"}")
                },
                new UpsertItem
                {
                    Id = "sample-2",
                    Collection = "docs",
                    Content = "Fish live in water and are known for their swimming abilities.",
                    Metadata = JsonDocument.Parse("{\"source\":\"sample\",\"topic\":\"animals\"}")
                }
            };

            foreach (var d in docs)
            {
                await store.UpsertAsync(d);
            }

            Console.WriteLine("Upserted sample documents.");
        }
    }
}


