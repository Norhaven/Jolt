using JoltJson = Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jolt.Json.DotNet
{
    public class JoltJsonObjectConverter : JsonConverter<JsonObject>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(JoltJson.IJsonObject);
        }

        public override JsonObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            return (JsonObject)JsonObject.Parse(root.GetRawText());
        }

        public override void Write(Utf8JsonWriter writer, JsonObject value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
