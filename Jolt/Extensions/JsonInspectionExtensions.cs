using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Extensions
{
    public static class JsonInspectionExtensions
    {
        public static JsonArrayElementType DetermineElementTypes(this IEnumerable<IJsonToken> arrayElements)
        {
            var flags = JsonArrayElementType.None;

            foreach (var element in arrayElements)
            {
                if (element.Type == JsonTokenType.Array)
                {
                    flags |= JsonArrayElementType.Array;
                }
                else if (element.Type == JsonTokenType.Object)
                {
                    flags |= JsonArrayElementType.Object;
                }
                else if (element.Type == JsonTokenType.Value)
                {
                    flags |= element.AsValue().ValueType switch
                    {
                        JsonValueType.String => JsonArrayElementType.String,
                        JsonValueType.Number when element.AsValue().ToTypeOf<string>().Contains('.') => JsonArrayElementType.Decimal,
                        JsonValueType.Number => JsonArrayElementType.Integer,
                        JsonValueType.Boolean => JsonArrayElementType.Boolean,
                        JsonValueType.Null => JsonArrayElementType.Null,
                        _ => throw new ArgumentOutOfRangeException(nameof(element), $"Unable to determine array element type for unsupported element type '{element.Type}'")
                    };
                }
            }

            return flags;
        }
    }
}
