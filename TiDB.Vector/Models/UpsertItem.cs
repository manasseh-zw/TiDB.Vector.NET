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

        /// <summary>
        /// Tags for the document using the new Tag type for better developer experience.
        /// </summary>
        /// <example>
        /// var item = new UpsertItem
        /// {
        ///     Id = "doc-1",
        ///     Collection = "docs",
        ///     Content = "Sample content",
        ///     Tags = new[] { new Tag("Department", "Engineering"), new Tag("OrganizationId", "org-123") }
        /// };
        /// </example>
        public IEnumerable<Tag>? Tags { get; init; }

        /// <summary>
        /// Legacy tags property for backward compatibility.
        /// This property is deprecated. Use Tags instead for better type safety.
        /// </summary>
        [Obsolete("Use Tags property instead for better type safety and developer experience.")]
        public JsonDocument? TagsJson { get; init; }

        public float[]? Embedding { get; init; }
        public ContentType ContentType { get; init; } = ContentType.PlainText;
    }
}


