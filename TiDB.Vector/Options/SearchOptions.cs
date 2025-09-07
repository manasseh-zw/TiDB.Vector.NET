using TiDB.Vector.Models;

namespace TiDB.Vector.Options
{
    /// <summary>
    /// Options for advanced search filtering
    /// </summary>
    public sealed class SearchOptions
    {
        /// <summary>
        /// Filter by a specific collection. If null, searches all collections.
        /// </summary>
        public string? Collection { get; set; }

        /// <summary>
        /// Filter by tags using the new TagFilter type for better developer experience.
        /// This is more efficient than filtering metadata as it uses a dedicated JSON column.
        /// </summary>
        /// <example>
        /// // Filter by single collection and tags using new Tag type
        /// var searchOptions = new SearchOptions
        /// {
        ///     Collection = "engineering-docs",
        ///     TagFilter = new TagFilter(new[]
        ///     {
        ///         new Tag("OrganizationId", "org-123"),
        ///         new Tag("Department", "Engineering")
        ///     }, TagFilterMode.And)
        /// };
        /// 
        /// // Or using implicit conversion
        /// var searchOptions2 = new SearchOptions
        /// {
        ///     TagFilter = new Tag("Department", "Engineering")
        /// };
        /// </example>
        public TagFilter? TagFilter { get; set; }

        /// <summary>
        /// Legacy tag filters for backward compatibility.
        /// This property is deprecated. Use TagFilter instead for better type safety.
        /// </summary>
        [Obsolete("Use TagFilter property instead for better type safety and developer experience.")]
        public Dictionary<string, string>? TagFilters { get; set; }

        /// <summary>
        /// Legacy tag mode for backward compatibility.
        /// This property is deprecated. Use TagFilter.Mode instead.
        /// </summary>
        [Obsolete("Use TagFilter.Mode property instead.")]
        public TagFilterMode TagMode { get; set; } = TagFilterMode.And;
    }
}
