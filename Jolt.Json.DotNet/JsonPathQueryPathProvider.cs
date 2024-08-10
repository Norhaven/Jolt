using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Node = System.Text.Json.Nodes.JsonNode;
using JsonObject = System.Text.Json.Nodes.JsonObject;
using JsonArray = System.Text.Json.Nodes.JsonArray;
using System.Collections;

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
            if (queryMode == JsonQueryMode.StartFromRoot)
            {
                var root = GetRootNodeFrom(source, queryMode);
                return root.SelectTokenAtPath(queryPath);
            }

            var validQuerySources = new Stack<IJsonToken>(source.Reverse());

            while (validQuerySources.Count > 0)
            {
                var currentSource = validQuerySources.Pop();
                var result = currentSource.SelectTokenAtPath(queryPath);

                if (result is null)
                {
                    continue;
                }

                return result;
            }

            return default;
        }
    }
}
