using Jolt.Structure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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

        public T ToTypeOf<T>() => _token.ToObject<T>();

        public override string ToString() => _token?.ToString();

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
    }
}
