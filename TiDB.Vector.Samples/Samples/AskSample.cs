using TiDB.Vector.Core;

namespace TiDB.Vector.Samples.Samples
{
    internal static class AskSample
    {
        public static async Task RunAsync(TiDBVectorStore store)
        {
            var answer = await store.AskAsync("Who is Cristiano Ronaldo?", topK: 5);
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


