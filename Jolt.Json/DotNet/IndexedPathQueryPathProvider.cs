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
    public class IndexedPathQueryPathProvider : QueryPathProvider
    {
        private readonly IDictionary<string, Node> _rootQueryPaths;
        private readonly IDictionary<string, Node> _sourceQueryPaths;
        private readonly Node _rootNode;
        private readonly JsonQueryMode _queryMode;

        public IndexedPathQueryPathProvider()
        {
        //    _rootNode = rootNode;
        //    _queryMode = queryMode;
        //    _rootQueryPaths = Index(_rootNode);
        //    _sourceQueryPaths = (sourceNode is null || rootNode == sourceNode) ? _rootQueryPaths : Index(sourceNode);
        }
        
        public override IJsonToken? SelectNodeAtPath(Stack<IJsonToken> closureSources, string path, JsonQueryMode queryMode)
        {
            // TODO: Implement correctly.

            return null; // return closureSources.Peek().SelectNodeAtPath(path);
        }

        private static IDictionary<string, Node?> Index(Node? node)
        {
            var index = new Dictionary<string, Node?>();
            var search = new Queue<(string Pointer, Node? Value)>();
            search.Enqueue((string.Empty, node));

            while (search.Any())
            {
                var current = search.Dequeue();
                index[current.Pointer] = current.Value;
                switch (current.Value)
                {
                    case JsonObject obj:
                        index[current.Pointer] = obj;
                        foreach (var kvp in obj)
                        {
                            search.Enqueue(($"{current.Pointer}/{Encode(kvp.Key)}", kvp.Value));
                        }
                        break;
                    case JsonArray arr:
                        index[current.Pointer] = arr;
                        for (var i = 0; i < arr.Count; i++)
                        {
                            var value = arr[i];
                            search.Enqueue(($"{current.Pointer}/{i}", value));
                        }

                        break;
                }
            }

            return index;
        }

        private static string Encode(string value)
        {
            if (value.All(c => c != '~' && c != '/'))
            {
                return value;
            }

            var builder = new StringBuilder();

            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '~':
                        builder.Append("~0");
                        break;
                    case '/':
                        builder.Append("~1");
                        break;
                    default:
                        builder.Append(ch);
                        break;
                }
            }

            return builder.ToString();
        }

        public override bool IsQueryPath(string path)
        {
            throw new NotImplementedException();
        }
    }
}
