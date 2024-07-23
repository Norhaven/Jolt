using Jolt.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Json.Newtonsoft
{
    public class JsonTokenReader : IJsonTokenReader
    {
        public IJsonToken? CreateArrayFrom(IEnumerable<IJsonToken> tokens)
        {
            var array = new JArray();

            foreach (var token in tokens)
            {
                array.Add(JToken.Parse(token.ToString()));
            }

            return JsonToken.FromObject(array);
        }

        public IJsonToken? CreateTokenFrom(object? value)
        {
            if (value is null)
            {
                return JsonToken.FromObject(JValue.CreateNull());
            }

            return JsonToken.FromObject(JToken.FromObject(value));
        }

        public IJsonToken? Read(string json)
        {
            return JsonToken.Parse(json);
        }
    }
}
