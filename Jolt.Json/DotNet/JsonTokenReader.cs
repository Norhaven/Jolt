using Jolt.Structure;
using Node = System.Text.Json.Nodes.JsonNode;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.Json.Nodes;

namespace Jolt.Json.DotNet
{
    public sealed class JsonTokenReader : IJsonTokenReader
    {
        public IJsonToken? CreateArrayFrom(IEnumerable<IJsonToken> tokens)
        {
            throw new NotImplementedException();
        }

        public IJsonToken? CreateObjectFrom(IEnumerable<IJsonToken>? resultValue)
        {
            throw new NotImplementedException();
        }

        public IJsonToken? CreateTokenFrom(object? value)
        {
            throw new NotImplementedException();
        }

        public IJsonToken Read(string json)
        {
            var rootToken = Node.Parse(json);
            //var pathQueryProvider = new IndexedPathQueryPathProvider(rootToken, null, JsonQueryMode.SearchFromRootNode);

            // TODO: Implement this.

            return null; // return new JsonNode(rootToken);
        }
    }
}
