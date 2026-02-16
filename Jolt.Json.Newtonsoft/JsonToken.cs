using Jolt.Structure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Json.Newtonsoft
{
    public abstract class JsonToken : IJsonToken
    {
        public static IJsonToken? Parse(string json)
        {
            var token = JToken.Parse(json);

            return FromObject(token);
        }

        public static IJsonToken? FromObject(JToken? token)
        {
            if (token is null)
            {
                return default;
            }

            return token.Type switch
            {
                JTokenType.Object => new JsonObject(token),
                JTokenType.Array => new JsonArray(token),
                JTokenType.Property => new JsonProperty(token),
                JTokenType.String => new JsonValue(token),
                JTokenType.Integer => new JsonValue(token),
                JTokenType.Boolean => new JsonValue(token),
                JTokenType.Float => new JsonValue(token),
                JTokenType.Null => new JsonValue(token),
                _ => throw new ArgumentOutOfRangeException(nameof(token), $"Unable to parse JSON token from object with unsupported type '{token.Type}'"),
            };
        }

        protected readonly JToken? _token;

        public IJsonToken? Parent => FromObject(_token?.Parent);
        public string? PropertyName => _token?.Path.Split('.')[^1];

        public JsonTokenType Type { get; }

        public JToken UnderlyingNode => _token;

        public JsonToken(JToken? token)
        {
            _token = token;

            if (_token is null)
            {
                Type = JsonTokenType.Value;
            }
            else
            {
                Type = _token.Type switch
                {
                    JTokenType.Object => JsonTokenType.Object,
                    JTokenType.Array => JsonTokenType.Array,
                    JTokenType.Property => JsonTokenType.Property,
                    JTokenType.String => JsonTokenType.Value,
                    JTokenType.Integer => JsonTokenType.Value,
                    JTokenType.Boolean => JsonTokenType.Value,
                    JTokenType.Float => JsonTokenType.Value,
                    JTokenType.Null => JsonTokenType.Null,
                    _ => throw new ArgumentOutOfRangeException(nameof(token), $"Unable to determine best JSON token type for unsupported type '{token.Type}'")
                };
            }
        }

        public abstract void Clear();

        public IJsonArray AsArray() => (JsonArray)this;
        public IJsonObject AsObject() => (JsonObject)this;
        public IJsonValue AsValue() => (JsonValue)this;

        public IJsonToken SelectTokenAtPath(string path) => FromObject(_token?.SelectToken(path));

        public IJsonToken? Copy()
        {
            var copiedToken = _token?.DeepClone();

            return FromObject(copiedToken);
        }

        public object ToTypeOf(Type type)
        {
            if (type == typeof(string))
            {
                return _token.ToString();
            }
            else if (type == typeof(object))
            {
                if (_token is JObject || _token is JArray)
                {
                    return Parse(_token.DeepClone().ToString());
                }
            }
            
            var serializer = new JsonSerializer
            {
                Converters =
                {
                    new JoltJsonObjectConverter(),
                    new JoltJsonArrayConverter()
                },
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            return _token.ToObject(type, serializer);
        }

        public T ToTypeOf<T>()
        {
            return (T)ToTypeOf(typeof(T));
        }

        public override string ToString() => _token?.ToString(Formatting.None);

        public override bool Equals(object obj)
        {
            if (obj is JsonToken json)
            {
                return JToken.DeepEquals(_token, json._token);
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _token.GetHashCode();
        }

        public virtual bool DeepEquals(IJsonToken otherToken, params IJsonEqualityComparer[] comparers)
        {
            var comparersByApplicableTypes = (comparers ?? Array.Empty<IJsonEqualityComparer>()).ToDictionary(x => x.ApplicableType, x => x);

            var thisIsNull = _token is null || _token.Type == JTokenType.Null;
            var otherIsNull = otherToken is null || otherToken.Type == JsonTokenType.Null;

            if (thisIsNull && otherIsNull)
            {
                return true;
            }

            if (thisIsNull || otherIsNull)
            {
                return false;
            }

            if (_token is JValue && otherToken is IJsonValue val)
            {
                if (comparersByApplicableTypes.TryGetValue(typeof(IJsonValue), out var comparer))
                {
                    return comparer.AreEqual(this, otherToken);
                }

                object? actualValue = _token.Type switch
                {
                    JTokenType.Boolean => _token.Value<bool>(),
                    JTokenType.Float => _token.Value<double>(),
                    JTokenType.Integer => _token.Value<long>(),
                    JTokenType.String => _token.Value<string>(),
                    JTokenType.Null => null,
                    _ => throw new InvalidOperationException($"Unexpected JTokenType '{_token.Type}' for value token")
                };

                return actualValue?.Equals(val.ToTypeOf<object>()) == true;
            }
            else if (this is IJsonObject && otherToken is IJsonObject obj)
            {
                if (comparersByApplicableTypes.TryGetValue(typeof(IJsonObject), out var comparer))
                {
                    return comparer.AreEqual(this, otherToken);
                }

                var thisProperties = ((IJsonObject)this).ToDictionary(p => p.PropertyName, p => p.Value);
                var otherProperties = obj.ToDictionary(p => p.PropertyName, p => p.Value);

                if (thisProperties.Count != otherProperties.Count)
                {
                    return false;
                }

                foreach (var property in thisProperties)
                {
                    if (!otherProperties.TryGetValue(property.Key, out var otherValue))
                    {
                        return false;
                    }

                    if (property.Value?.DeepEquals(otherValue, comparers) != true)
                    {
                        return false;
                    }
                }

                return true;
            }
            else if (this is IJsonArray && otherToken is IJsonArray array)
            {
                var thisItems = AsArray();

                if (thisItems.Length != array.Length)
                {
                    return false;
                }

                for (int i = 0; i < thisItems.Length; i++)
                {
                    if (thisItems[i]?.DeepEquals(array[i]) != true)
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
    }
}
