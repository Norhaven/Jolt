using JoltJson = Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jolt.Json.DotNet
{
    public class JoltJsonArrayConverter : JsonConverter<JsonArray>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(JoltJson.IJsonArray);
        }

        public override JsonArray? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            return (JsonArray)JsonToken.Parse(root.GetRawText());
        }

        public override void Write(Utf8JsonWriter writer, JsonArray value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
