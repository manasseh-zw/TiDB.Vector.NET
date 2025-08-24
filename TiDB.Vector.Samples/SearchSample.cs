using TiDB.Vector.Core;
using TiDB.Vector.OpenAI.Builder;

namespace TiDB.Vector.Samples.Samples
{
    internal static class SearchSample
    {
        public static async Task RunAsync()
        {
            await SearchCollectionAsync("docs_txt", "What does jane confess to elizabeth?");
            await SearchCollectionAsync("docs_md", "recite the to earth wind poem");
            await SearchCollectionAsync("docs_html", "What is ronaldos highest scoring game?");
        }

        private static async Task SearchCollectionAsync(string collection, string query)
        {
            var connString = AppConfig.TiDBConnectionString;

            var store = new TiDBVectorStoreBuilder(connString)
                .WithDefaultCollection(collection)
                .WithDistanceFunction(DistanceFunction.Cosine)
                .AddOpenAI(AppConfig.OpenAIApiKey)
                .AddOpenAITextEmbedding(AppConfig.EmbeddingModel, 1536)
                .AddOpenAIChatCompletion(AppConfig.CompletionModel)
                .EnsureSchema(createVectorIndex: true)
                .Build();

            var results = await store.SearchAsync(query, topK: 3);
            Console.WriteLine(
                $"Collection: {collection} - Top {results.Count} results for '{query}':"
            );
            foreach (var r in results)
            {
                Console.WriteLine($"- {r.Id} | distance={r.Distance:0.0000} | {r.Content}");
            }
            Console.WriteLine();
        }
    }
}
