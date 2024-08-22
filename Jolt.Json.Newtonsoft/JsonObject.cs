using Jolt.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Jolt.Json.Newtonsoft
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
                _properties[propertyName] = value;
                ((JObject)_token)[propertyName] = value?.ToTypeOf<JToken>();
            }
        }

        public JsonObject(JToken? token) 
            : base(token)
        {
            _properties = ((JObject)_token).Properties().ToDictionary(x => x.Name, x => FromObject(x.Value));
        }

        public IJsonToken? Remove(string propertyName)
        {
            if (!_properties.TryGetValue(propertyName, out var propertyToken))
            {
                return default;
            }

            _properties.Remove(propertyName);
            ((JObject)_token).Remove(propertyName);

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
                var current = _token;

                var underlyingValue = value switch
                {
                    JsonToken token => token.UnderlyingNode,
                    _ => throw new ArgumentOutOfRangeException(nameof(path), $"Encountered non-JSON node at path '{path}'")
                };

                for (var i = 0; i < pathParts.Length; i++)
                {
                    var property = pathParts[i];
                    var isFinalProperty = i == pathParts.Length - 1;

                    var newObject = isFinalProperty ? underlyingValue : new JObject();

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

        public IEnumerator<IJsonProperty> GetEnumerator() => ((JObject)_token).Properties().Select(x => (IJsonProperty)FromObject(x)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override void Clear()
        {
            _properties.Clear();
            ((JObject)_token).RemoveAll();
        }
    }
}
