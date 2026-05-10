using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;
using Nodes = System.Text.Json.Nodes;
using JsonValueKind = System.Text.Json.JsonValueKind;
using NamingPolicy = System.Text.Json.JsonNamingPolicy;
using System.Linq;
using System.Xml.Linq;
using Json.Path;
using JsonElement = System.Text.Json.JsonElement;
using JsonSerializer = System.Text.Json.JsonSerializer;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace Jolt.Json.DotNet
{
    public abstract class JsonToken : IJsonToken
    {
        public static IJsonToken? Parse(string json)
        {
            var token = Nodes.JsonNode.Parse(json);

            return FromObject(token);
        }

        public static IJsonToken? FromObject(Nodes.JsonNode? token)
        {
            if (token is null)
            {
                return default;
            }

            return token switch
            {
                Nodes.JsonObject _ => new JsonObject(token),
                Nodes.JsonArray _ => new JsonArray(token),
                Nodes.JsonValue value when value.GetValueKind() == JsonValueKind.String => new JsonValue(token),
                Nodes.JsonValue value when value.GetValueKind() == JsonValueKind.Number => new JsonValue(token),
                Nodes.JsonValue value when value.GetValueKind() == JsonValueKind.True => new JsonValue(token),
                Nodes.JsonValue value when value.GetValueKind() == JsonValueKind.False => new JsonValue(token),
                Nodes.JsonValue value when value.GetValueKind() == JsonValueKind.Null => new JsonValue(token),
                _ => throw new ArgumentOutOfRangeException(nameof(token), $"Unable to parse JSON token from object with unsupported type '{token.GetValueKind()}'"),
            };
        }

        protected readonly Nodes.JsonNode? _token;

        public IJsonToken? Parent => FromObject(_token?.Parent);
        public string? PropertyName => _token?.GetPath().Split('.')[^1];

        public JsonTokenType Type { get; }

        public Nodes.JsonNode? UnderlyingNode => _token;

        public JsonToken(Nodes.JsonNode? token)
        {
            _token = token;

            if (_token is null)
            {
                Type = JsonTokenType.Value;
            }
            else
            {
                Type = _token.GetValueKind() switch
                {
                    JsonValueKind.Object => JsonTokenType.Object,
                    JsonValueKind.Array => JsonTokenType.Array,
                    JsonValueKind.String => JsonTokenType.Value,
                    JsonValueKind.Number => JsonTokenType.Value,
                    JsonValueKind.True => JsonTokenType.Value,
                    JsonValueKind.False => JsonTokenType.Value,
                    JsonValueKind.Null => JsonTokenType.Null,
                    _ => throw new ArgumentOutOfRangeException(nameof(token), $"Unable to determine best JSON token type for unsupported type '{token.GetValueKind()}'")
                };
            }
        }

        public abstract void Clear();

        public IJsonArray AsArray() => (JsonArray)this;
        public IJsonObject AsObject() => (JsonObject)this;
        public IJsonValue AsValue() => (JsonValue)this;

        public IJsonToken SelectTokenAtPath(string path) => FromObject(SelectToken(path));

        public IJsonToken? Copy()
        {
            var copiedToken = _token?.DeepClone();

            return FromObject(copiedToken);
        }

        public object ToTypeOf(Type type)
        {
            // Due to the System.Text.Json library's result when using GetValue<object>() where it passes back
            // the JsonElement in question instead of the underlying value as an object, we're doing a quick hack
            // here to use the generic ToTypeOf<T>() method instead of the other way around.

            var method = GetType().GetMethod(nameof(ToTypeOf), System.Type.EmptyTypes)?.MakeGenericMethod(type);

            return method.Invoke(this, null);
        }

        public T ToTypeOf<T>()
        {
            if (typeof(T) == typeof(object))
            {
                if (_token is Nodes.JsonObject || _token is Nodes.JsonArray)
                {
                    return (T)Parse(_token.ToJsonString());
                }

                var value = _token.GetValue<object?>();

                if (value is JsonElement element)
                {
                    value = ((JsonElement)_token.GetValue<object>()) switch
                    {
                        var x when x.ValueKind == JsonValueKind.String => x.GetString(),
                        var x when x.ValueKind == JsonValueKind.Number && x.TryGetInt64(out var integerValue) => integerValue,
                        var x when x.ValueKind == JsonValueKind.Number && x.TryGetDouble(out var doubleValue) => doubleValue,
                        var x when x.ValueKind == JsonValueKind.True => x.GetBoolean(),
                        var x when x.ValueKind == JsonValueKind.False => x.GetBoolean(),
                        var x when x.ValueKind == JsonValueKind.Null => null,
                        var x when x.ValueKind == JsonValueKind.Array => JsonSerializer.Deserialize<T[]>(_token),
                        var x when x.ValueKind == JsonValueKind.Object => JsonSerializer.Deserialize<T>(_token),
                        _ => throw new ArgumentOutOfRangeException($"Unable to get JSON value as a System.Object class")
                    };
                }
                else
                {
                    return _token.GetValue<T>();
                }

                return (T)value;
            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)_token.ToString();
            }
            else if (_token is Nodes.JsonObject || _token is Nodes.JsonArray)
            {
                var options = new JsonSerializerOptions
                {
                    Converters =
                    {
                        new JoltJsonObjectConverter(),
                        new JoltJsonArrayConverter()
                    },
                    PropertyNamingPolicy = NamingPolicy.CamelCase
                };

                return JsonSerializer.Deserialize<T>(_token.ToJsonString(), options);
            }

            return _token.GetValue<T>();
        }

        public override string ToString() => _token?.ToJsonString(new JsonSerializerOptions { WriteIndented = false });

        private Nodes.JsonNode? SelectToken(string path)
        {
            if (!JsonPath.TryParse(path, out var query))
            {
                return default;
            }

            var result = query.Evaluate(_token);

            if (result.Matches.Count == 0)
            {
                return default;
            }

            return result.Matches[0].Value;
        }

        public override bool Equals(object obj)
        {
            if (obj is IJsonToken token)
            {
                return DeepEquals(token);
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

            var thisIsNull = _token is null || _token.GetValueKind() == JsonValueKind.Null;
            var otherIsNull = otherToken is null || otherToken.Type == JsonTokenType.Null;

            if (thisIsNull && otherIsNull)
            {
                return true;
            }

            if (thisIsNull || otherIsNull)
            {
                return false;
            }

            if (this is IJsonValue && otherToken is IJsonValue val)
            {
                if (comparersByApplicableTypes.TryGetValue(typeof(IJsonValue), out var comparer))
                {
                    return comparer.AreEqual(this, otherToken);
                }

                object? actualValue = _token.GetValueKind() switch
                {
                    JsonValueKind.True => _token.GetValue<bool>(),
                    JsonValueKind.False => _token.GetValue<bool>(),
                    JsonValueKind.Number when _token.TryGetValue<long>(out var longVal) => longVal,
                    JsonValueKind.Number when _token.TryGetValue<double>(out var doubleVal) => doubleVal,
                    JsonValueKind.String => _token.GetValue<string>(),
                    JsonValueKind.Null => null,
                    _ => throw new InvalidOperationException($"Unexpected JsonValueKind '{_token.GetValueKind()}' for value token")
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

                    if (property.Value is null && otherValue is null)
                    {
                        return true;
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
                    if (thisItems[i] is null && array[i] is null)
                    {
                        continue;
                    }

                    if (thisItems[i]?.DeepEquals(array[i], comparers) != true)
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
