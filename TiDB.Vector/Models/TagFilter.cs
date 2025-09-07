namespace TiDB.Vector.Models
{
    /// <summary>
    /// Represents a tag filter for search operations
    /// </summary>
    public sealed record TagFilter
    {
        /// <summary>
        /// Collection of tags to filter by
        /// </summary>
        public IReadOnlyList<Tag> Tags { get; init; } = Array.Empty<Tag>();

        /// <summary>
        /// How to combine multiple tag filters. Default is And (all must match).
        /// </summary>
        public TagFilterMode Mode { get; init; } = TagFilterMode.And;

        /// <summary>
        /// Creates a new TagFilter with the specified tags
        /// </summary>
        /// <param name="tags">Tags to filter by</param>
        /// <param name="mode">How to combine the tags</param>
        public TagFilter(IEnumerable<Tag> tags, TagFilterMode mode = TagFilterMode.And)
        {
            Tags = tags.ToList().AsReadOnly();
            Mode = mode;
        }

        /// <summary>
        /// Creates a new TagFilter with a single tag
        /// </summary>
        /// <param name="key">Tag key</param>
        /// <param name="value">Tag value</param>
        /// <param name="mode">How to combine with other filters</param>
        public TagFilter(string key, string value, TagFilterMode mode = TagFilterMode.And)
            : this(new[] { new Tag(key, value) }, mode)
        {
        }

        /// <summary>
        /// Creates a TagFilter from a dictionary (for backward compatibility)
        /// </summary>
        /// <param name="tagDictionary">Dictionary of key-value pairs</param>
        /// <param name="mode">How to combine the tags</param>
        public static TagFilter FromDictionary(IReadOnlyDictionary<string, string> tagDictionary, TagFilterMode mode = TagFilterMode.And)
        {
            var tags = tagDictionary.Select(kvp => new Tag(kvp.Key, kvp.Value));
            return new TagFilter(tags, mode);
        }

        /// <summary>
        /// Converts to dictionary format (for backward compatibility)
        /// </summary>
        /// <returns>Dictionary representation of the tags</returns>
        public IReadOnlyDictionary<string, string> ToDictionary()
        {
            return Tags.ToDictionary(tag => tag.Key, tag => tag.Value);
        }

        /// <summary>
        /// Implicit conversion from Tag array to TagFilter
        /// </summary>
        public static implicit operator TagFilter(Tag[] tags) => new(tags);

        /// <summary>
        /// Implicit conversion from single Tag to TagFilter
        /// </summary>
        public static implicit operator TagFilter(Tag tag) => new(new[] { tag });
    }

    /// <summary>
    /// How to combine multiple tag filters
    /// </summary>
    public enum TagFilterMode
    {
        /// <summary>
        /// All tag filters must match (AND logic)
        /// </summary>
        And,

        /// <summary>
        /// Any tag filter can match (OR logic)
        /// </summary>
        Or
    }
}
