using TiDB.Vector.Core;
using TiDB.Vector.OpenAI.Builder;

namespace TiDB.Vector.Samples.Samples
{
    internal static class AskSample
    {
        public static async Task RunAsync()
        {
            var connString = AppConfig.TiDBConnectionString;

            var store = new TiDBVectorStoreBuilder(connString)
                .WithDefaultCollection("docs_txt")
                .WithDistanceFunction(DistanceFunction.Cosine)
                .AddOpenAI(AppConfig.OpenAIApiKey)
                .AddOpenAITextEmbedding(AppConfig.EmbeddingModel, 1536)
                .AddOpenAIChatCompletion(AppConfig.CompletionModel)
                .EnsureSchema(createVectorIndex: true)
                .Build();

            var answer = await store.AskAsync(
                "What is the highest scoring game for Ronaldo?",
                topK: 5
            );
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
