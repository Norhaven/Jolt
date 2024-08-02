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
                    _ => throw new ArgumentOutOfRangeException(nameof(token), $"Unable to determine best JSON value type for unsupported type '{_token.Type}'")
                };
            }
        }

        public bool IsObject<T>() => ((JValue)_token).Value?.GetType() == typeof(T);
        public T ToTypeOf<T>() => _token.ToObject<T>();

        public override void Clear()
        {
            ((JValue)_token).Value = default;
        }
    }
}
