using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;
using Nodes = System.Text.Json.Nodes;
using JsonValueKind = System.Text.Json.JsonValueKind;
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
                Nodes.JsonObject obj => new JsonObject(token),
                Nodes.JsonArray array => new JsonArray(token),
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

        public T ToTypeOf<T>()
        {
            if (typeof(T) == typeof(object))
            {
                if (_token is Nodes.JsonObject)
                {
                    return JsonSerializer.Deserialize<T>(_token.ToJsonString());
                }

                object? value = ((JsonElement)_token.GetValue<object>()) switch
                {
                    var x when x.ValueKind == JsonValueKind.String => x.GetString(),
                    var x when x.ValueKind == JsonValueKind.Number => x.GetDecimal(),
                    var x when x.ValueKind == JsonValueKind.True => x.GetBoolean(),
                    var x when x.ValueKind == JsonValueKind.False => x.GetBoolean(),
                    var x when x.ValueKind == JsonValueKind.Null => null,
                    var x when x.ValueKind == JsonValueKind.Array => JsonSerializer.Deserialize<T[]>(_token),
                    var x when x.ValueKind == JsonValueKind.Object => JsonSerializer.Deserialize<T>(_token),
                    _ => throw new ArgumentOutOfRangeException($"Unable to get JSON value as a System.Object class")
                };

                return (T)value;
            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)_token.ToString();
            }

            return _token.GetValue<T>();
        }

        public override string ToString() => _token?.ToJsonString();

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
    }
}
