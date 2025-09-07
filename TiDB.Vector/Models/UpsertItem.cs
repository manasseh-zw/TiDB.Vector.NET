using System;
using System.Text.Json;

namespace TiDB.Vector.Models
{
    public enum ContentType
    {
        PlainText = 0,
        Markdown = 1,
        Html = 2
    }

    public sealed record UpsertItem
    {
        public string Id { get; init; } = string.Empty;
        public string Collection { get; init; } = string.Empty;
        public string? Content { get; init; }
        public JsonDocument? Metadata { get; init; }
        public string? Source { get; init; }
        public JsonDocument? Tags { get; init; }
        public float[]? Embedding { get; init; }
        public ContentType ContentType { get; init; } = ContentType.PlainText;
    }
}


