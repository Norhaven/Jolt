using Jolt.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;

namespace Jolt.Json.Newtonsoft
{
    public sealed class JsonPathQueryPathProvider : QueryPathProvider
    {
        // Converting a source document takes long enough to matter when it's queried repeatedly (e.g. a filter within a loop).

        private readonly ConditionalWeakTable<JToken, JsonNode> _convertedDocuments = new ConditionalWeakTable<JToken, JsonNode>();

        public override bool IsQueryPath(string path)
        {
            // We're not doing an exhaustive validation here, it's enough for now to say that it
            // seems to indicate a path and let the caller try to use it to test whether it's valid.

            return path?.StartsWith("$") == true;
        }

        public override bool IsValidQueryPath(string path) => global::Json.Path.JsonPath.TryParse(path, out _);

        public override bool CanMatchMultipleValues(string path) => global::Json.Path.JsonPath.TryParse(path, out var query) && !query.IsSingular;

        protected override IJsonToken? SelectFrom(IJsonToken source, string path, bool isSourceDocument)
        {
            return isSourceDocument && source is JsonToken token && token.UnderlyingNode != null
                ? JsonPathEvaluator.Select(token.UnderlyingNode, path, _convertedDocuments)
                : source.SelectTokenAtPath(path);
        }
    }
}
