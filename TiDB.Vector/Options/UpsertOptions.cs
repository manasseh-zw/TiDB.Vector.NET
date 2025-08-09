namespace TiDB.Vector.Options
{
    public sealed record UpsertOptions
    {
        public bool UseChunking { get; init; } = false;
        public bool Overwrite { get; init; } = true;
        public int MaxTokensPerChunk { get; init; } = 600;
        public int OverlapTokens { get; init; } = 80;
        public string? ChunkHeader { get; init; }
    }
}


