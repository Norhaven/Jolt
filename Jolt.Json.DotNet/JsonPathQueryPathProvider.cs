using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Node = System.Text.Json.Nodes.JsonNode;
using JsonObject = System.Text.Json.Nodes.JsonObject;
using JsonArray = System.Text.Json.Nodes.JsonArray;

namespace Jolt.Json.DotNet
{
    public sealed class JsonPathQueryPathProvider : QueryPathProvider
    {
        public override bool IsQueryPath(string path)
        {
            // We're not doing an exhaustive validation here, it's enough for now to say that it
            // seems to indicate a path and let the caller try to use it to test whether it's valid.

            return path?.StartsWith("$") == true;
        }

        public override IJsonToken? SelectNodeAtPath(Stack<IJsonToken> source, string queryPath, JsonQueryMode queryMode)
        {
            var queryToken = queryMode switch
            {
                JsonQueryMode.StartFromRoot => GetRootNodeFrom(source, queryMode),
                JsonQueryMode.StartFromClosestMatch => source.Peek(),
                _ => throw new ArgumentOutOfRangeException(nameof(queryPath), $"Unable to select JSON node, found unsupported query mode '{queryMode}'")
            };

            return queryToken.SelectTokenAtPath(queryPath);
        }
    }
}
