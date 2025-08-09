namespace TiDB.Vector.Models
{
    public sealed record Citation
    {
        public string Id { get; init; } = string.Empty;
        public string? Snippet { get; init; }
        public double Distance { get; init; }
    }
}


