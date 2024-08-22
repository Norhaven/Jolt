using Jolt.Structure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using Nodes = System.Text.Json.Nodes;

namespace Jolt.Json.DotNet
{
    public sealed class JsonObject : JsonToken, IJsonObject
    {
        private readonly IDictionary<string, IJsonToken> _properties = new Dictionary<string, IJsonToken>();

        public IJsonToken? this[string propertyName]
        {
            get
            {
                if (_properties.TryGetValue(propertyName, out var token))
                {
                    return token;
                }

                return default;
            }
            set
            {
                var unpackedNode = value switch
                {
                    null => default,
                    JsonToken token => token.UnderlyingNode,
                    _ => throw new ArgumentOutOfRangeException(nameof(propertyName), $"Unable to set property '{propertyName}' on JSON object due to failed unpacking of inner token")
                };

                ((Nodes.JsonObject)_token)[propertyName] = unpackedNode?.DeepClone();

                _properties[propertyName] = FromObject(((Nodes.JsonObject)_token)[propertyName]);
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

        public IJsonToken? RemoveAtPath(string path)
        {
            var nodePendingRemoval = SelectTokenAtPath(path);

            if (nodePendingRemoval is null)
            {
                return default;
            }

            var nodeParent = nodePendingRemoval.Parent;

            if (nodeParent is IJsonObject obj)
            {
                var property = obj.First(x => x.Value == nodePendingRemoval).PropertyName;

                return obj.Remove(property);
            }

            return default;
        }

        public IJsonToken? AddAtPath(string path, IJsonToken? value)
        {
            var pathParts = path.Split('.');
            var parentPath = string.Join('.', pathParts[..^1]);
            var propertyName = pathParts[^1];

            var parent = SelectTokenAtPath(parentPath);

            if (parent is null)
            {
                var current = UnderlyingNode;

                var underlyingValue = value switch
                {
                    JsonToken token => token.UnderlyingNode,
                    _ => throw new Exception()
                };

                for (var i = 0; i < pathParts.Length; i++)
                {
                    var property = pathParts[i];
                    var isFinalProperty = i == pathParts.Length - 1;

                    var newObject = isFinalProperty ? underlyingValue : new Nodes.JsonObject();

                    current[property] = newObject;

                    if (isFinalProperty)
                    {
                        return this;
                    }
                    else
                    {
                        current = newObject;
                    }
                }
            }

            if (parent is IJsonObject obj)
            {
                obj[propertyName] = value;
                return value;
            }

            return default;
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
