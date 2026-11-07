using Jolt.Structure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Jolt.Extensions;
using System.Text;
using Nodes = System.Text.Json.Nodes;

namespace Jolt.Json.DotNet
{
    public sealed class JsonArray : JsonToken, IJsonArray
    {
        private readonly IList<IJsonToken> _arrayElements;

        public IJsonToken? this[int index] 
        {
            get => _arrayElements[index];
            set
            {
                var node = CloneForArray(value);

                _token.AsArray()[index] = node;
                _arrayElements[index] = FromObject(node) ?? JsonValue.CreateNull();
            }
        }

        public JsonArrayElementType ElementTypes => _arrayElements.DetermineElementTypes();

        public int Length => _arrayElements.Count;

        public JsonArray(Nodes.JsonNode? token) 
            : base(token)
        {
            _arrayElements = ((Nodes.JsonArray)token).Select(x => FromObject(x) ?? JsonValue.CreateNull()).ToList();
        }

        public void Add(IJsonToken? token)
        {
            if (token is null)
            {
                return;
            }

            var node = CloneForArray(token);

            _token.AsArray().Add(node);
            _arrayElements.Add(FromObject(node) ?? JsonValue.CreateNull());
        }

        public IEnumerator<IJsonToken> GetEnumerator() => _arrayElements.GetEnumerator();

        public IJsonToken? RemoveAt(int index)
        {
            var element = this[index];

            _arrayElements.RemoveAt(index);

            return element;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // System.Text.Json only allows a node to have one parent, and a token may still belong to
        // another document (e.g. the source), so what goes into an array is always its own copy.

        private static Nodes.JsonNode? CloneForArray(IJsonToken? token) => (token as JsonToken)?.UnderlyingNode?.DeepClone();

        public override void Clear()
        {
            _arrayElements?.Clear();
            ((Nodes.JsonArray)_token).Clear();
        }
    }
}
