using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;
using Nodes = System.Text.Json.Nodes;
using JsonValueKind = System.Text.Json.JsonValueKind;
using System.Text.Json;

namespace Jolt.Json.DotNet
{
    public sealed class JsonValue : JsonToken, IJsonValue
    {
        /// <summary>
        /// Creates a token for a JSON null. System.Text.Json represents these as a null node, so this
        /// gives them a token of their own, the same as one that the Newtonsoft provider would give.
        /// </summary>
        public static JsonValue CreateNull() => new JsonValue(default, Structure.JsonTokenType.Null);

        public JsonValueType ValueType { get; }

        public JsonValue(Nodes.JsonNode? token) 
            : this(token, Structure.JsonTokenType.Value)
        {
        }

        private JsonValue(Nodes.JsonNode? token, Structure.JsonTokenType typeWithoutNode)
            : base(token, typeWithoutNode)
        {
            if (_token is null)
            {
                ValueType = JsonValueType.Null;
            }
            else
            {
                ValueType = _token.GetValueKind() switch
                {
                    JsonValueKind.String => JsonValueType.String,
                    JsonValueKind.Number => JsonValueType.Number,
                    JsonValueKind.True => JsonValueType.Boolean,
                    JsonValueKind.False => JsonValueType.Boolean,
                    JsonValueKind.Null => JsonValueType.Null,
                    _ => throw new ArgumentOutOfRangeException(nameof(token), $"Unable to determine best JSON value type for unsupported type '{_token.GetValueKind()}'")
                };
            }
        }

        public bool IsObject<T>() => ToTypeOf<object>()?.GetType() == typeof(T);

        public override void Clear()
        {
            ((Nodes.JsonValue)_token)?.ReplaceWith<string>(default);
        }

        // JSON values are a special case where we can't just write the token itself with ToJsonString()
        // because it will leave in the double quotes around a string value, so we're handling
        // that case for values by default by using the token's ToString() method instead which has a
        // special case built into it for that.

        public override string ToString() => _token?.ToString();

        public override bool Equals(object obj)
        {
            return base.Equals(obj) &&
                obj is JsonValue other &&
                ValueType == other.ValueType &&
                object.Equals(ToTypeOf<object>(), other.ToTypeOf<object>());
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ ValueType.GetHashCode() ^ (ToTypeOf<object>()?.GetHashCode() ?? 0);
        }
    }
}
