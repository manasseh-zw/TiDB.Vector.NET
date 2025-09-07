using System.Text.Json;
using System.Text.Json.Nodes;

namespace TiDB.Vector.Models
{
    /// <summary>
    /// Represents a key-value tag for vector documents
    /// </summary>
    public sealed record Tag(string Key, string Value)
    {
        /// <summary>
        /// Creates a JSON document from a collection of tags
        /// </summary>
        /// <param name="tags">Collection of tags to convert</param>
        /// <returns>JSON document representation of the tags</returns>
        public static JsonDocument? ToJsonDocument(IEnumerable<Tag>? tags)
        {
            if (tags == null || !tags.Any())
                return null;

            var jsonObject = new JsonObject();
            foreach (var tag in tags)
            {
                jsonObject[tag.Key] = tag.Value;
            }

            return JsonDocument.Parse(jsonObject.ToJsonString());
        }

        /// <summary>
        /// Creates a collection of tags from a JSON document
        /// </summary>
        /// <param name="jsonDocument">JSON document containing tag data</param>
        /// <returns>Collection of tags</returns>
        public static IEnumerable<Tag> FromJsonDocument(JsonDocument? jsonDocument)
        {
            if (jsonDocument == null)
                return Enumerable.Empty<Tag>();

            var tags = new List<Tag>();
            foreach (var property in jsonDocument.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    tags.Add(new Tag(property.Name, property.Value.GetString() ?? string.Empty));
                }
            }

            return tags;
        }

        /// <summary>
        /// Implicit conversion from (string, string) tuple to Tag
        /// </summary>
        public static implicit operator Tag((string key, string value) tuple) => new(tuple.key, tuple.value);
    }
}
