using System.Text.Json;
using TiDB.Vector.Core;
using TiDB.Vector.Models;
using TiDB.Vector.OpenAI.Builder;
using TiDB.Vector.Options;

namespace TiDB.Vector.Samples.Samples
{
    internal static class UpsertSample
    {
        public static async Task RunAsync()
        {
            var apiKey = AppConfig.OpenAIApiKey;
            var connString = AppConfig.TiDBConnectionString;

            var store = new TiDBVectorStoreBuilder(connString)
                .WithDefaultCollection("docs")
                .WithDistanceFunction(DistanceFunction.Cosine)
                .AddOpenAITextEmbedding(
                    apiKey: apiKey,
                    embeddingModel: "text-embedding-3-small",
                    dimension: 1536
                )
                .AddOpenAIChatCompletion(apiKey: apiKey, chatModel: "gpt-4.1")
                .EnsureSchema(createVectorIndex: true)
                .Build();

            await store.EnsureSchemaAsync();

            var txt = File.ReadAllText(Path.Combine("Samples", "content", "content.txt"));
            var md = File.ReadAllText(Path.Combine("Samples", "content", "content.md"));
            var html = File.ReadAllText(Path.Combine("Samples", "content", "content.html"));

            var docs = new[]
            {
                new UpsertItem
                {
                    Id = "txt-1",
                    Collection = "docs_txt",
                    Content = txt,
                    Metadata = JsonDocument.Parse("{\"source\":\"sample\",\"format\":\"txt\"}"),
                    ContentType = ContentType.PlainText,
                },
                new UpsertItem
                {
                    Id = "md-1",
                    Collection = "docs_md",
                    Content = md,
                    Metadata = JsonDocument.Parse("{\"source\":\"sample\",\"format\":\"md\"}"),
                    ContentType = ContentType.Markdown,
                },
                new UpsertItem
                {
                    Id = "html-1",
                    Collection = "docs_html",
                    Content = html,
                    Metadata = JsonDocument.Parse("{\"source\":\"sample\",\"format\":\"html\"}"),
                    ContentType = ContentType.Html,
                },
            };

            var options = new UpsertOptions
            {
                UseChunking = true,
                MaxTokensPerChunk = 600,
                OverlapTokens = 80,
                ChunkHeader = "Sample: content",
                StripHtml = true,
            };

            foreach (var d in docs)
            {
                await store.UpsertAsync(d, options);
            }

            Console.WriteLine(
                "Upserted content files with chunking into docs_txt, docs_md, docs_html."
            );
        }
    }
}
