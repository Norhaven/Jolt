using Jolt.Structure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using JsonPath = global::Json.Path.JsonPath;
using IndexSelector = global::Json.Path.IndexSelector;
using NameSelector = global::Json.Path.NameSelector;

namespace Jolt.Json.Newtonsoft
{
    /// <summary>
    /// Evaluates JSON paths using the same engine as the System.Text.Json provider, so that both give the same results. A path that
    /// can match one value at most is made up of only names and indexes, which can be followed directly on the tokens. Anything else
    /// is evaluated against a converted copy of the tokens, and each match's location is followed back to the token that it came from.
    /// </summary>
    internal static class JsonPathEvaluator
    {
        public static IJsonToken? Select(JToken source, string path, ConditionalWeakTable<JToken, JsonNode>? convertedDocuments = default)
        {
            if (!JsonPath.TryParse(JsonPathText.NormalizeRelative(path), out var query))
            {
                return default;
            }

            if (query.IsSingular)
            {
                var token = Follow(source, query);

                // Newtonsoft hands back an object with a Type of Null instead of just returning null like System.Text.Json, so
                // we use the default value for JsonValue, which correctly sets Type to Value and ValueType to Null. Otherwise, both
                // Type and ValueType will be Null, which is incorrect and confusing to the users.

                return token?.Type == JTokenType.Null ? new JsonValue(default) : JsonToken.FromObject(token);
            }

            var document = convertedDocuments is null ? Convert(source) : convertedDocuments.GetValue(source, Convert);
            var matches = new JArray();

            foreach (var match in query.Evaluate(document).Matches)
            {
                matches.Add(Follow(source, match.Location));
            }

            return JsonToken.FromObject(matches);
        }

        private static JToken? Follow(JToken source, JsonPath path)
        {
            var current = source;

            foreach (var segment in path.Segments)
            {
                current = current is JProperty property ? property.Value : current;

                current = segment.Selectors[0] switch
                {
                    NameSelector name when current is JObject obj => obj.TryGetValue(name.Name, StringComparison.Ordinal, out var value) ? value : default,
                    IndexSelector index when current is JArray array => ElementAt(array, index.Index),
                    _ => default
                };

                if (current is null)
                {
                    return default;
                }
            }

            return current is JProperty lastProperty ? lastProperty.Value : current;
        }

        private static JToken? ElementAt(JArray array, int index)
        {
            var actualIndex = index < 0 ? array.Count + index : index;

            return actualIndex >= 0 && actualIndex < array.Count ? array[actualIndex] : default;
        }

        private static JsonNode? Convert(JToken token)
        {
            // The conversion only exists for the duration of the evaluation, so it shouldn't limit how deep the source document can be.

            var options = new JsonDocumentOptions { MaxDepth = int.MaxValue };

            return JsonNode.Parse(token.ToString(Formatting.None, Array.Empty<JsonConverter>()), default, options);
        }
    }
}
