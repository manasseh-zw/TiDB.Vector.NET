using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TiDB.Vector.Abstractions
{
    public interface ITextGenerator
    {
        Task<string> CompleteAsync(
            string system,
            IReadOnlyList<(string role, string content)> messages,
            CancellationToken cancellationToken = default);
    }
}


