using TiDB.Vector.Core;
using TiDB.Vector.OpenAI.Builder;
using TiDB.Vector.Samples;

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
            var config = new TiDB.Vector.OpenAI.OpenAIConfig
            {
                ApiKey = AppConfig.OpenAIApiKey,
                Model = "text-embedding-3-small",
                Dimension = 1536
            };
            var connString = AppConfig.TiDBConnectionString;

            var store = new TiDBVectorStoreBuilder(connString)
                .WithDefaultCollection(collection)
                .WithDistanceFunction(DistanceFunction.Cosine)
                .AddOpenAITextEmbedding(apiKey: config.ApiKey, model: config.Model, dimension: config.Dimension)
                .AddOpenAIChatCompletion(apiKey: config.ApiKey, model: "gpt-4.1")
                .EnsureSchema(createVectorIndex: true)
                .Build();

			var results = await store.SearchAsync(query, topK: 3);
			Console.WriteLine($"Collection: {collection} - Top {results.Count} results for '{query}':");
			foreach (var r in results)
			{
				Console.WriteLine($"- {r.Id} | distance={r.Distance:0.0000} | {r.Content}");
			}
			Console.WriteLine();
		}
	}
}


