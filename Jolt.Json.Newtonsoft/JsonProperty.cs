using Jolt.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Json.Newtonsoft
{
    public sealed class JsonProperty : JsonToken, IJsonProperty
    {
        public string PropertyName => ((JProperty)_token).Name;

        public IJsonToken? Value 
        {
            get => FromObject(((JProperty)_token).Value);
            set => ((JProperty)_token).Value = value.ToTypeOf<JToken>();
        }

        public JsonProperty(JToken? token)
            : base(token)
        {
        }

        public override void Clear()
        {
            Value = default;
        }
    }
}
