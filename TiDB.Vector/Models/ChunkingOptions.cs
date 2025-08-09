namespace TiDB.Vector.Models
{
    public sealed record ChunkingOptions
    {
        public int TargetTokens { get; init; } = 600;
        public int OverlapTokens { get; init; } = 80;
    }
}


