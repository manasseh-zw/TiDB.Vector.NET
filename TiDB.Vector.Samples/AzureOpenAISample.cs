using TiDB.Vector.AzureOpenAI.Builder;
using TiDB.Vector.Core;

namespace TiDB.Vector.Samples.Samples
{
    internal static class AzureOpenAISample
    {
        public static async Task RunAsync()
        {
            await SearchWithAzureOpenAIAsync("docs_txt", "What does jane confess to elizabeth?");
            await AskWithAzureOpenAIAsync("What animals can swim?");
        }

        private static async Task SearchWithAzureOpenAIAsync(string collection, string query)
        {
            var apiKey = AppConfig.AzureOpenAIApiKey;
            var endpoint = AppConfig.AzureOpenAIEndpoint;
            var embeddingDeployment = AppConfig.AzureOpenAIEmbeddingDeployment;
            var connString = AppConfig.TiDBConnectionString;

            var store = new TiDBVectorStoreBuilder(connString)
                .WithDefaultCollection(collection)
                .WithDistanceFunction(DistanceFunction.Cosine)
                .AddAzureOpenAITextEmbedding(
                    apiKey: apiKey,
                    endpoint: endpoint,
                    deploymentName: embeddingDeployment,
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
            var apiKey = AppConfig.AzureOpenAIApiKey;
            var endpoint = AppConfig.AzureOpenAIEndpoint;
            var embeddingDeployment = AppConfig.AzureOpenAIEmbeddingDeployment;
            var chatDeployment = AppConfig.AzureOpenAIChatDeployment;
            var connString = AppConfig.TiDBConnectionString;

            var store = new TiDBVectorStoreBuilder(connString)
                .WithDefaultCollection("docs_txt")
                .WithDistanceFunction(DistanceFunction.Cosine)
                .AddAzureOpenAITextEmbedding(
                    apiKey: apiKey,
                    endpoint: endpoint,
                    deploymentName: embeddingDeployment,
                    dimension: 1536
                )
                .AddAzureOpenAIChatCompletion(
                    apiKey: apiKey,
                    endpoint: endpoint,
                    deploymentName: chatDeployment
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
