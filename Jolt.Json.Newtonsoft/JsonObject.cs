using Jolt.Structure;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Jolt.Extensions;

namespace Jolt.Json.Newtonsoft
{
    public sealed class JsonObject : JsonToken, IJsonObject
    {
        public IJsonToken? this[string propertyName]
        {
            get
            {
                var token = ((JObject)_token)[propertyName];

                // The System.Text.Json property retrieval returns a null value in the case of a null JSON value,
                // so we will do the same here for consistency.

                return token?.Type == JTokenType.Null ? default : FromObject(token);
            }
            set
            {
                ((JObject)_token)[propertyName] = value?.ToTypeOf<JToken>();
            }
        }

        public JsonObject(JToken? token)
            : base(token)
        {
        }

        public IJsonToken? Remove(string propertyName)
        {
            var propertyToken = FromObject(((JObject)_token)[propertyName]);

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
            var parentPath = pathParts[..^1].Join('.');
            var propertyName = pathParts[^1];

            var parent = SelectTokenAtPath(parentPath);

            // A property containing a JSON null has nothing to add to, so it's replaced in the same way as a missing one.

            if (parent is null || (parent is IJsonValue parentValue && parentValue.ValueType == JsonValueType.Null))
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

        public bool HasProperty(string propertyName) => ((JObject)_token).ContainsKey(propertyName);

        public IEnumerator<IJsonProperty> GetEnumerator() => ((JObject)_token).Properties().Select(x => (IJsonProperty)FromObject(x)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override void Clear()
        {
            ((JObject)_token).RemoveAll();
        }

        public IJsonObject? MergeWith(IJsonObject? otherObject)
        {
            if (otherObject is null)
            {
                return this;
            }

            if (!(otherObject is JsonObject otherJsonObject))
            {
                throw new ArgumentOutOfRangeException(nameof(otherObject), $"Unable to merge with provided object of type '{otherObject.GetType().FullName}' - expected type was '{typeof(JsonObject).FullName}'");
            }

            var result = (JObject)_token.DeepClone();

            MergeInto(result, (JObject)otherJsonObject._token);

            return new JsonObject(result);
        }

        public string? ToMinifiedString()
        {
            return _token?.ToString(Formatting.None);
        }

        private static void MergeInto(JObject target, JObject overrides)
        {
            foreach (var property in overrides.Properties())
            {
                if (target.TryGetValue(property.Name, out var existingValue)
                    && existingValue.Type == JTokenType.Object
                    && property.Value.Type == JTokenType.Object)
                {
                    // Both sides are objects — recurse rather than replace.
                    MergeInto((JObject)existingValue, (JObject)property.Value);
                }
                else
                {
                    // Override wins: scalar, array, type mismatch, or new key.
                    target[property.Name] = property.Value.DeepClone();
                }
            }
        }
    }
}
