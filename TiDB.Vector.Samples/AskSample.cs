using TiDB.Vector.Core;
using TiDB.Vector.OpenAI.Builder;
using TiDB.Vector.Samples;

namespace TiDB.Vector.Samples.Samples
{
	internal static class AskSample
	{
		public static async Task RunAsync()
		{
			var config = new TiDB.Vector.OpenAI.OpenAIConfig
			{
				ApiKey = AppConfig.OpenAIApiKey,
				Model = "text-embedding-3-small",
				Dimension = 1536
			};
			var connString = AppConfig.TiDBConnectionString;

			var store = new TiDBVectorStoreBuilder(connString)
				.WithDefaultCollection("docs_txt")
				.WithDistanceFunction(DistanceFunction.Cosine)
				.AddOpenAITextEmbedding(apiKey: config.ApiKey, model: config.Model, dimension: config.Dimension)
				.AddOpenAIChatCompletion(apiKey: config.ApiKey, model: "gpt-4.1")
				.EnsureSchema(createVectorIndex: true)
				.Build();

			var answer = await store.AskAsync("What is the highest scoring game for Ronaldo?", topK: 5);
			Console.WriteLine("Answer:\n" + answer.Text);
			Console.WriteLine();
			Console.WriteLine("Sources:");
			foreach (var c in answer.Sources)
			{
				Console.WriteLine($"- {c.Id} (distance={c.Distance:0.0000})");
			}
		}
	}
}


