using Jolt.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Json.Newtonsoft
{
    public sealed class JsonValue : JsonToken, IJsonValue
    {
        public JsonValueType ValueType { get; }

        public JsonValue(JToken? token) 
            : base(token)
        {
            if (_token is null)
            {
                ValueType = JsonValueType.Null;
            }
            else
            {
                ValueType = _token.Type switch
                {
                    JTokenType.String => JsonValueType.String,
                    JTokenType.Integer => JsonValueType.Number,
                    JTokenType.Boolean => JsonValueType.Boolean,
                    JTokenType.Float => JsonValueType.Number,
                    JTokenType.Null => JsonValueType.Null,
                    JTokenType.Date => JsonValueType.String,
                    JTokenType.TimeSpan => JsonValueType.String,
                    _ => throw new ArgumentOutOfRangeException(nameof(token), $"Unable to determine best JSON value type for unsupported type '{_token.Type}'")
                };
            }
        }

        public bool IsObject<T>() => ((JValue)_token).Value?.GetType() == typeof(T);
        public T ToTypeOf<T>() => _token is null ? default : _token.ToObject<T>();

        public override void Clear()
        {
            ((JValue)_token).Value = default;
        }

        // JSON values are a special case where we can't just write the token itself to a string
        // because it will leave in the double quotes around a string value, so we're handling
        // that case for values by default.

        public override string ToString()
        {
            // In the case where the underlying value is a boolean, Newtonsoft will return "True" or "False" instead of "true" or "false"
            // and so we need to handle that case here to ensure parity with the JSON spec and the System.Text.Json implementation.

            return _token?.Value<string>() switch
            {
                "True" => "true",
                "False" => "false",
                var x => x
            };
        }
    }
}
