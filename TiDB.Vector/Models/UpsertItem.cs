using System;
using System.Text.Json;

namespace TiDB.Vector.Models
{
    public sealed record UpsertItem
    {
        public string Id { get; init; } = string.Empty;
        public string Collection { get; init; } = string.Empty;
        public string? Content { get; init; }
        public JsonDocument? Metadata { get; init; }
        public float[]? Embedding { get; init; }
    }
}


