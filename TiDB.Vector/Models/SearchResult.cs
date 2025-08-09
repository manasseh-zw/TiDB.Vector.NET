using System.Text.Json;

namespace TiDB.Vector.Models
{
    public sealed record SearchResult
    {
        public string Id { get; init; } = string.Empty;
        public string Collection { get; init; } = string.Empty;
        public string? Content { get; init; }
        public JsonDocument? Metadata { get; init; }
        public double Distance { get; init; }
    }
}


