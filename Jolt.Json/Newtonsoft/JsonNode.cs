using Jolt.Exceptions;
using Jolt.Json.Newtonsoft.Extensions;
using Jolt.Structure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Jolt.Json.Newtonsoft
{
    public sealed class JsonNode : IJsonNode
    {
        private JProperty? _property;
        private JToken? _value;

        public IJsonNode? Parent
        {
            get
            {
                if (IsProperty)
                {
                    return new JsonNode(_property?.Parent);
                }

                if (_value?.Parent is null)
                {
                    return default;
                }

                if (_value?.Parent is JProperty property)
                {
                    return new JsonNode(property);
                }

                return new JsonNode(_value?.Parent);
            }
        }

        public string? PropertyName => _property?.Name;

        public object? Value 
        {
            get => _property?.Value?.ToObject<object>();
            set => _property.Value = JToken.FromObject(value);
        }

        public JsonNodeType Type
        {
            get
            {
                return (_property?.Value?.Type ?? _value?.Type) switch
                {
                    JTokenType.Array => JsonNodeType.Array,
                    JTokenType.Object => JsonNodeType.Object,
                    JTokenType.String => JsonNodeType.String,
                    JTokenType.Integer => JsonNodeType.Number,
                    JTokenType.Boolean => JsonNodeType.Boolean,
                    _ => JsonNodeType.Unknown
                };
            }
        }

        public bool IsProperty => _property != null;

        public bool IsValue => _value != null;

        public IJsonNode this[int index]
        {
            get
            {
                if (Type != JsonNodeType.Array)
                {
                    throw new InvalidOperationException($"Unable to get an element by index, type '{Type}' is not an array");
                }

                var array = (JArray)(_property?.Value ?? _value);

                return new JsonNode(array[index]);
            }
        }

        public IJsonNode this[string propertyName]
        {
            get
            {
                if (Type != JsonNodeType.Object)
                {
                    throw new InvalidOperationException($"Unable to get property by name, type '{Type}' is not an object");
                }

                var obj = (JObject)(_property?.Value ?? _value);
                var newProperty = obj.Property(propertyName);

                return new JsonNode(newProperty);
            }
        }

        public JsonNode(JProperty? property)
        {
            _property = property;
        }

        public JsonNode(JToken? value)
        {
            _value = value;
        }

        public IEnumerator<IJsonNode> GetEnumerator() => CreateEnumerator();

        public IJsonNode? SelectNodeAtPath(string path)
        {
            var selectedToken = (_property?.Value ?? _value).SelectToken(path);

            if (selectedToken is null)
            {
                return null;
            }

            return new JsonNode(selectedToken);
        }

        public IJsonNode? GetChildTemplate()
        {
            var array = IsProperty ? (JArray)_property.Value : (JArray)_value;
            var templateToken = array[0];

            array.Remove(templateToken);

            var template = new JsonNode(templateToken);

            return template;
        }

        public void AddChild(IJsonNode? node)
        {
            if (node is null)
            {
                return;
            }

            if (Type == JsonNodeType.Array)
            {
                if (node is JsonNode tokenNode)
                {
                    var array = (JArray)(_property?.Value ?? _value);

                    array.Add(tokenNode._value);

                    return;
                }
            }

            throw new JoltExecutionException($"Unable to add node '{node.Type}' as a child to token of type '{_value.Type}'");
        }

        public IJsonNode? Copy(string? propertyName = default)
        {
            var node = (_property?.Value ?? _value);

            if (node is null)
            {
                return default;
            }

            string? copiedToken = default;

            if (Type == JsonNodeType.Object)
            {
                var keyValuePairs = JsonConvert.DeserializeObject<Dictionary<string, object>>(node.ToString());

                if (!string.IsNullOrWhiteSpace(propertyName))
                {
                    keyValuePairs.Remove(PropertyName);
                    keyValuePairs[propertyName] = Value;
                }

                copiedToken = JsonConvert.SerializeObject(keyValuePairs);
            }
            else if (Type == JsonNodeType.Array)
            {
                var keyValuePairs = JsonConvert.DeserializeObject<Dictionary<string, object>>(Parent.ToString());

                if (!string.IsNullOrWhiteSpace(propertyName))
                {
                    keyValuePairs.Remove(PropertyName);
                    keyValuePairs[propertyName] = Value;
                }

                copiedToken = JsonConvert.SerializeObject(keyValuePairs);
            }

            var parsedToken = JToken.Parse(copiedToken);

            if (IsProperty)
            {
                return new JsonNode(((JObject)parsedToken).Property(propertyName));
            }

            return new JsonNode(parsedToken);
        }

        IEnumerator IEnumerable.GetEnumerator() => CreateEnumerator();

        public override string ToString()
        {
            return (_property?.Value ?? _value).ToString();
        }

        private IEnumerator<IJsonNode> CreateEnumerator()
        {
            if (Type == JsonNodeType.Array)
            {
                return ((JArray)_property?.Value ?? _value).Select(x => new JsonNode(x)).GetEnumerator();
            }
            else if (Type == JsonNodeType.Object)
            {
                return ((JObject)(_property?.Value ?? _value)).Properties().Select(x => new JsonNode(x)).GetEnumerator();
            }

            return Enumerable.Empty<IJsonNode>().GetEnumerator();
        }

        public IJsonNode? ApplyChange(object? value)
        {
            // TODO: Fix ApplyChange to mutate correctly on down the chain and not just replace and throw away.

            var tokenValue = value is null ? JValue.CreateNull() : ((JsonNode)value)._value;

            if (IsProperty)
            {
                _property.Value = tokenValue;
            }
            else if (IsValue)
            {
                if (_value.Parent is null)
                {
                    _value = tokenValue;
                }
                else if (_value.Parent.Type == JTokenType.Property)
                {
                    ((JProperty)_value.Parent).Value = tokenValue;
                }
            }

            return this;
        }

        public IJsonNode? ApplyChange(string propertyName, object? value)
        {
            if (typeof(IEnumerable<IJsonNode>).IsAssignableFrom(value?.GetType()))
            {
                var collectedResults = ((IEnumerable<IJsonNode>)value).OfType<JsonNode>().Select(x => x._value).ToArray();
                var collectedResultsJson = new JArray(collectedResults);

                if (IsProperty)
                {
                    var copy = Copy(propertyName);

                    _property = ((JsonNode)copy)._property;
                    _property.Value = collectedResultsJson;
                }
            }
            else
            {
                var copy = Copy(propertyName);

                _property = ((JsonNode)copy)._property;
                _property.Value = JToken.FromObject(value);
            }

            return this;
        }

        public IJsonNode? ApplyChange(IJsonNode? node)
        {
            var jsonNode = (JsonNode)node;

            if (IsProperty)
            {
                _property = jsonNode._property;
            }
            else if (IsValue)
            {
                _value = jsonNode._value;
            }

            return this;
        }
    }
}
