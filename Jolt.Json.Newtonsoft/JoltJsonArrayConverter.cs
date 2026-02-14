using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Jolt.Structure;

namespace Jolt.Json.Newtonsoft
{
    public class JoltJsonArrayConverter : JsonConverter<IJsonArray>
    {
        public override IJsonArray? ReadJson(JsonReader reader, Type objectType, IJsonArray? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jsonObject = JArray.Load(reader);

            return (JsonArray)JsonToken.Parse(jsonObject.ToString());
        }

        public override void WriteJson(JsonWriter writer, IJsonArray? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
