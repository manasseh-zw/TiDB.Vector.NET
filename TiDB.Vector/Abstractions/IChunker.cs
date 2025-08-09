using System.Collections.Generic;
using TiDB.Vector.Models;

namespace TiDB.Vector.Abstractions
{
    public interface IChunker
    {
        IReadOnlyList<Chunk> Chunk(string text, ChunkingOptions options);
    }
}


