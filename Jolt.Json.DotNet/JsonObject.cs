using Jolt.Structure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nodes = System.Text.Json.Nodes;

namespace Jolt.Json.DotNet
{
    public sealed class JsonObject : JsonToken, IJsonObject
    {
        private readonly IDictionary<string, IJsonToken> _properties = new Dictionary<string, IJsonToken>();

        public IJsonToken? this[string propertyName]
        {
            get => _properties[propertyName];
            set
            {
                _properties[propertyName] = value;

                var unpackedNode = value switch
                {
                    null => default,
                    JsonToken token => token.UnderlyingNode,
                    _ => throw new ArgumentOutOfRangeException(nameof(propertyName), $"Unable to set property '{propertyName}' on JSON object due to failed unpacking of inner token")
                };

                ((Nodes.JsonObject)_token)[propertyName] = unpackedNode?.DeepClone();
            }
        }

        public JsonObject(Nodes.JsonNode? token) 
            : base(token)
        {
            _properties = ((Nodes.JsonObject)_token).ToDictionary(x => x.Key, x => FromObject(x.Value));
        }

        public IJsonToken? Remove(string propertyName)
        {
            if (!_properties.TryGetValue(propertyName, out var propertyToken))
            {
                return default;
            }

            _properties.Remove(propertyName);
            ((Nodes.JsonObject)_token).Remove(propertyName);

            return propertyToken;
        }

        public IEnumerator<IJsonProperty> GetEnumerator() => ((Nodes.JsonObject)_token).Select(x => new JsonProperty(x.Value)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override void Clear()
        {
            _properties.Clear();
            ((Nodes.JsonObject)_token).Clear();
        }
    }
}
