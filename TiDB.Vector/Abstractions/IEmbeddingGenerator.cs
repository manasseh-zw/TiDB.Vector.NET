using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TiDB.Vector.Abstractions
{
    public interface IEmbeddingGenerator
    {
        int Dimension { get; }

        Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<float[]>> GenerateBatchAsync(
            IEnumerable<string> texts,
            CancellationToken cancellationToken = default);
    }
}


