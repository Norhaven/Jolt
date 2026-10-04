using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Jolt.LanguageServer.Mapping
{
    /// <summary>
    /// Represents the location of a property (its key and value) or array element (its value only) at a JSON path.
    /// </summary>
    internal sealed class JsonPathEntry
    {
        public JsonPathEntry(JsonStringSyntax? key, JsonSyntaxNode value)
        {
            Key = key;
            Value = value;
        }

        public JsonStringSyntax? Key { get; }

        public JsonSyntaxNode Value { get; }
    }

    /// <summary>
    /// Maps the JSON paths that Jolt reports validation issues with (from System.Text.Json's JsonNode.GetPath, e.g.
    /// "$.a.b[0]" or "$['#foreach(@x in $.items)']") to where those values are in the document.
    /// </summary>
    internal sealed class JsonPathIndex
    {
        private static readonly ConcurrentDictionary<string, string> _propertySegments = new ConcurrentDictionary<string, string>();

        private readonly Dictionary<string, JsonPathEntry> _entries = new Dictionary<string, JsonPathEntry>();

        public JsonPathIndex(JsonSyntaxNode root)
        {
            Root = root;
            _entries["$"] = new JsonPathEntry(null, root);
            Add("$", root);
        }

        public JsonSyntaxNode Root { get; }

        /// <summary>
        /// Gets every string key and string value in the document, in document order.
        /// </summary>
        public List<JsonStringSyntax> Strings { get; } = new List<JsonStringSyntax>();

        public JsonPathEntry? Find(string? path) => path != null && _entries.TryGetValue(path, out var entry) ? entry : null;

        private void Add(string path, JsonSyntaxNode node)
        {
            switch (node)
            {
                case JsonObjectSyntax obj:
                    foreach (var property in obj.Properties)
                    {
                        var propertyPath = path + GetPropertySegment(property.Key.Value);

                        Strings.Add(property.Key);

                        // A later duplicate property replaces an earlier one, as it would when the JSON is read.
                        _entries[propertyPath] = new JsonPathEntry(property.Key, property.Value);
                        Add(propertyPath, property.Value);
                    }
                    break;

                case JsonArraySyntax array:
                    for (var i = 0; i < array.Items.Count; i++)
                    {
                        var itemPath = $"{path}[{i}]";

                        _entries[itemPath] = new JsonPathEntry(null, array.Items[i]);
                        Add(itemPath, array.Items[i]);
                    }
                    break;

                case JsonStringSyntax str:
                    Strings.Add(str);
                    break;
            }
        }

        /// <summary>
        /// Gets the path segment for a property name exactly as System.Text.Json formats it (".name" or "['name']"),
        /// by asking it for the path of a property with that name rather than reproducing its quoting rules.
        /// </summary>
        private static string GetPropertySegment(string name)
        {
            // Property names change with every keystroke while one is being edited, so the cache is kept from growing
            // without bound over a long session.
            if (_propertySegments.Count > MaxCachedSegments)
            {
                _propertySegments.Clear();
            }

            return _propertySegments.GetOrAdd(name, x =>
            {
                var obj = new JsonObject { [x] = 0 };

                return obj[x]!.GetPath().Substring(1);
            });
        }

        private const int MaxCachedSegments = 10000;
    }
}
