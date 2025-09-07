using System.Text.Json;
using TiDB.Vector.Core;
using TiDB.Vector.Models;
using TiDB.Vector.Options;
using TiDB.Vector.OpenAI.Builder;

namespace TiDB.Vector.Samples;

/// <summary>
/// Demonstrates advanced filtering capabilities including collection filtering,
/// key-value tag filtering, and source tracking.
/// </summary>
public static class FilteringSample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("🔍 Advanced Filtering Sample");
        Console.WriteLine("=============================");

        // Build the vector store with OpenAI
        var store = new TiDBVectorStoreBuilder(AppConfig.TiDBConnectionString)
            .AddOpenAI(AppConfig.OpenAIApiKey)
            .AddOpenAITextEmbedding("text-embedding-3-small", 1536)
            .AddOpenAIChatCompletion("gpt-4.1")
            .Build();

        // Ensure schema is created
        await store.EnsureSchemaAsync();

        // Sample documents with different organizations and departments
        var documents = new[]
        {
            new UpsertItem
            {
                Id = "doc-1",
                Collection = "engineering-docs",
                Content = "How to implement microservices architecture with Docker and Kubernetes.",
                Source = "https://docs.company.com/microservices-guide.pdf",
                Metadata = JsonDocument.Parse("""
                {
                    "Category": "Architecture",
                    "Tags": ["microservices", "docker", "kubernetes"]
                }
                """),
                Tags = JsonDocument.Parse("""
                {
                    "OrganizationId": "org-123",
                    "Department": "Engineering"
                }
                """)
            },
            new UpsertItem
            {
                Id = "doc-2",
                Collection = "engineering-docs",
                Content = "Best practices for code review and pull request management.",
                Source = "https://docs.company.com/code-review-guide.pdf",
                Metadata = JsonDocument.Parse("""
                {
                    "Category": "Process",
                    "Tags": ["code-review", "git", "process"]
                }
                """),
                Tags = JsonDocument.Parse("""
                {
                    "OrganizationId": "org-123",
                    "Department": "Engineering"
                }
                """)
            },
            new UpsertItem
            {
                Id = "doc-3",
                Collection = "hr-docs",
                Content = "Employee onboarding process and company policies.",
                Source = "https://hr.company.com/onboarding.pdf",
                Metadata = JsonDocument.Parse("""
                {
                    "Category": "Process",
                    "Tags": ["onboarding", "policies", "hr"]
                }
                """),
                Tags = JsonDocument.Parse("""
                {
                    "OrganizationId": "org-123",
                    "Department": "HR"
                }
                """)
            },
            new UpsertItem
            {
                Id = "doc-4",
                Collection = "engineering-docs",
                Content = "Database optimization techniques for high-performance applications.",
                Source = "https://docs.company.com/db-optimization.pdf",
                Metadata = JsonDocument.Parse("""
                {
                    "Category": "Performance",
                    "Tags": ["database", "optimization", "performance"]
                }
                """),
                Tags = JsonDocument.Parse("""
                {
                    "OrganizationId": "org-456",
                    "Department": "Engineering"
                }
                """)
            },
            new UpsertItem
            {
                Id = "doc-5",
                Collection = "marketing-docs",
                Content = "Social media marketing strategies and campaign management.",
                Source = "https://marketing.company.com/social-media-guide.pdf",
                Metadata = JsonDocument.Parse("""
                {
                    "Category": "Strategy",
                    "Tags": ["social-media", "marketing", "campaigns"]
                }
                """),
                Tags = JsonDocument.Parse("""
                {
                    "OrganizationId": "org-456",
                    "Department": "Marketing"
                }
                """)
            }
        };

        // Upsert all documents
        Console.WriteLine("📝 Upserting sample documents...");
        await store.UpsertBatchAsync(documents);
        Console.WriteLine($"✅ Upserted {documents.Length} documents");

        // Wait a moment for indexing
        await Task.Delay(2000);

        Console.WriteLine("\n🔍 Search Examples:");
        Console.WriteLine("==================");

        // 1. Search all documents (no filtering)
        Console.WriteLine("\n1️⃣ Search all documents:");
        var allResults = await store.SearchAsync("software development practices", topK: 3);
        foreach (var result in allResults)
        {
            Console.WriteLine($"   📄 {result.Id} | {result.Collection} | Source: {result.Source}");
            Console.WriteLine($"      Distance: {result.Distance:F4}");
        }

        // 2. Filter by collection
        Console.WriteLine("\n2️⃣ Filter by collection (engineering-docs only):");
        var engineeringResults = await store.SearchAsync(
            "software development practices",
            topK: 3,
            new SearchOptions { Collection = "engineering-docs" }
        );
        foreach (var result in engineeringResults)
        {
            Console.WriteLine($"   📄 {result.Id} | {result.Collection} | Source: {result.Source}");
            Console.WriteLine($"      Distance: {result.Distance:F4}");
        }

        // 3. Filter by OrganizationId (key-value filtering)
        Console.WriteLine("\n3️⃣ Filter by OrganizationId (org-123 only):");
        var org123Results = await store.SearchAsync(
            "processes and practices",
            topK: 3,
            new SearchOptions
            {
                TagFilters = new Dictionary<string, string>
                {
                    ["OrganizationId"] = "org-123"
                }
            }
        );
        foreach (var result in org123Results)
        {
            Console.WriteLine($"   📄 {result.Id} | {result.Collection} | Source: {result.Source}");
            Console.WriteLine($"      Distance: {result.Distance:F4}");
        }

        // 4. Filter by multiple key-value pairs (AND logic)
        Console.WriteLine("\n4️⃣ Filter by OrganizationId AND Department (org-123 + Engineering):");
        var org123EngineeringResults = await store.SearchAsync(
            "development practices",
            topK: 3,
            new SearchOptions
            {
                TagFilters = new Dictionary<string, string>
                {
                    ["OrganizationId"] = "org-123",
                    ["Department"] = "Engineering"
                },
                TagMode = TagFilterMode.And
            }
        );
        foreach (var result in org123EngineeringResults)
        {
            Console.WriteLine($"   📄 {result.Id} | {result.Collection} | Source: {result.Source}");
            Console.WriteLine($"      Distance: {result.Distance:F4}");
        }

        // 5. Filter by different collection (hr-docs only)
        Console.WriteLine("\n5️⃣ Filter by different collection (hr-docs only):");
        var hrResults = await store.SearchAsync(
            "processes and policies",
            topK: 3,
            new SearchOptions { Collection = "hr-docs" }
        );
        foreach (var result in hrResults)
        {
            Console.WriteLine($"   📄 {result.Id} | {result.Collection} | Source: {result.Source}");
            Console.WriteLine($"      Distance: {result.Distance:F4}");
        }

        // 6. Filter by Department (Engineering only)
        Console.WriteLine("\n6️⃣ Filter by Department (Engineering only):");
        var engineeringOnlyResults = await store.SearchAsync(
            "best practices",
            topK: 3,
            new SearchOptions
            {
                TagFilters = new Dictionary<string, string>
                {
                    ["Department"] = "Engineering"
                }
            }
        );
        foreach (var result in engineeringOnlyResults)
        {
            Console.WriteLine($"   📄 {result.Id} | {result.Collection} | Source: {result.Source}");
            Console.WriteLine($"      Distance: {result.Distance:F4}");
        }

        // 7. Ask with filtering
        Console.WriteLine("\n7️⃣ Ask with filtering (Engineering department only):");
        var answer = await store.AskAsync(
            "What are the best practices for software development?",
            topK: 3,
            new SearchOptions
            {
                TagFilters = new Dictionary<string, string>
                {
                    ["Department"] = "Engineering"
                }
            }
        );
        Console.WriteLine($"   💬 Answer: {answer.Text}");
        Console.WriteLine($"   📚 Sources ({answer.Sources.Count}):");
        foreach (var source in answer.Sources)
        {
            Console.WriteLine($"      📄 {source.Id} | Source: {source.Source} | Distance: {source.Distance:F4}");
        }

        Console.WriteLine("\n✅ Advanced filtering sample completed!");
    }
}
