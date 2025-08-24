using TiDB.Vector.AzureOpenAI.Builder;
using TiDB.Vector.Core;

namespace TiDB.Vector.Samples.Samples
{
    internal static class AzureOpenAISample
    {
        public static async Task RunAsync()
        {
            await SearchWithAzureOpenAIAsync(
                "docs_txt",
                "what are the themes of pride and prejudice?"
            );
            await AskWithAzureOpenAIAsync("What is ronaldo's best performing?");
        }

        private static async Task SearchWithAzureOpenAIAsync(string collection, string query)
        {
            var store = new TiDBVectorStoreBuilder(AppConfig.TiDBConnectionString)
                .WithDefaultCollection(collection)
                .WithDistanceFunction(DistanceFunction.Cosine)
                .AddAzureOpenAITextEmbedding(
                    apiKey: AppConfig.AzureOpenAIApiKey,
                    endpoint: AppConfig.AzureOpenAIEndpoint,
                    embeddingModel: AppConfig.EmbeddingModel,
                    dimension: 1536
                )
                .EnsureSchema(createVectorIndex: true)
                .Build();

            var results = await store.SearchAsync(query, topK: 3);
            Console.WriteLine(
                $"Azure OpenAI - Collection: {collection} - Top {results.Count} results for '{query}':"
            );
            foreach (var r in results)
            {
                Console.WriteLine($"- {r.Id} | distance={r.Distance:0.0000} | {r.Content}");
            }
            Console.WriteLine();
        }

        private static async Task AskWithAzureOpenAIAsync(string query)
        {
            var store = new TiDBVectorStoreBuilder(AppConfig.TiDBConnectionString)
                .WithDefaultCollection("docs_html")
                .WithDistanceFunction(DistanceFunction.Cosine)
                .AddAzureOpenAITextEmbedding(
                    apiKey: AppConfig.AzureOpenAIApiKey,
                    endpoint: AppConfig.AzureOpenAIEndpoint,
                    embeddingModel: AppConfig.EmbeddingModel,
                    dimension: 1536
                )
                .AddAzureOpenAIChatCompletion(
                    apiKey: AppConfig.AzureOpenAIApiKey,
                    endpoint: AppConfig.AzureOpenAIEndpoint,
                    chatModel: AppConfig.CompletionModel
                )
                .EnsureSchema(createVectorIndex: true)
                .Build();

            var answer = await store.AskAsync(query, topK: 5);
            Console.WriteLine($"Azure OpenAI - Question: {query}");
            Console.WriteLine($"Answer: {answer.Text}");
            Console.WriteLine($"Sources: {answer.Sources.Count}");
            foreach (var source in answer.Sources)
            {
                Console.WriteLine($"- {source.Id} | distance={source.Distance:0.0000}");
            }
            Console.WriteLine();
        }
    }
}
