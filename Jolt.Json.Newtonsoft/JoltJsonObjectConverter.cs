using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Jolt.Structure;

namespace Jolt.Json.Newtonsoft
{
    public class JoltJsonObjectConverter : JsonConverter<IJsonObject>
    {
        public override IJsonObject? ReadJson(JsonReader reader, Type objectType, IJsonObject? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);

            return (JsonObject)JsonObject.Parse(jsonObject.ToString());
        }

        public override void WriteJson(JsonWriter writer, IJsonObject? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
