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
        /// Filter by key-value pairs in the dedicated tags JSON column. 
        /// Key is the tags field name, value is the expected string value.
        /// This is more efficient than filtering metadata as it uses a dedicated JSON column.
        /// </summary>
        /// <example>
        /// // Filter by single collection and tags
        /// var searchOptions = new SearchOptions
        /// {
        ///     Collection = "engineering-docs",
        ///     TagFilters = new Dictionary&lt;string, string&gt;
        ///     {
        ///         ["OrganizationId"] = "org-123",
        ///         ["Department"] = "Engineering"
        ///     }
        /// };
        /// </example>
        public Dictionary<string, string>? TagFilters { get; set; }

        /// <summary>
        /// How to combine multiple tag filters. Default is And (all must match).
        /// </summary>
        public TagFilterMode TagMode { get; set; } = TagFilterMode.And;
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
