using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;
using Nodes = System.Text.Json.Nodes;

namespace Jolt.Json.DotNet
{
    public sealed class JsonProperty : JsonToken, IJsonProperty
    {
        public string PropertyName { get; }

        public IJsonToken? Value 
        {
            get => FromObject(_token);
            set => _token.ReplaceWith(value.ToTypeOf<Nodes.JsonNode>());
        }

        public JsonProperty(Nodes.JsonNode? token)
            : base(token)
        {
            PropertyName = token.GetPropertyName();
        }

        public override void Clear()
        {
            Value = default;
        }
    }
}
