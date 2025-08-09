namespace TiDB.Vector.Models
{
    public sealed record Chunk
    {
        public string Text { get; init; } = string.Empty;
        public int StartOffset { get; init; }
        public int EndOffset { get; init; }
    }
}


