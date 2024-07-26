using Jolt.Exceptions;
using Jolt.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
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
                if (token is IJsonValue value)
                {
                    array.Add(value.AsObject<object>());
                }
                else
                {
                    array.Add(JToken.Parse(token.ToString()));
                }
            }

            return JsonToken.FromObject(array);
        }

        public IJsonToken? CreateObjectFrom(IEnumerable<IJsonToken> tokens)
        {
            var obj = new JObject();

            foreach(var token in tokens)
            {
                if (token is IJsonObject json)
                {
                    foreach (var property in json)
                    {
                        obj[property.PropertyName] = JToken.Parse(property.Value.ToString());
                    }
                }
                else if (token is IJsonProperty property)
                {
                    obj[property.PropertyName] = JToken.Parse(property.Value.ToString());
                }
                else
                {
                    throw new JoltExecutionException($"Unable to create JSON object from unsupported token type '{token.Type}'");
                }
            }

            return JsonToken.FromObject(obj);
        }

        public IJsonToken? CreateTokenFrom(object? value)
        {
            if (value is null)
            {
                return JsonToken.FromObject(JValue.CreateNull());
            }
            else if (value is IJsonToken token)
            {
                return token;
            }
            
            return JsonToken.FromObject(JToken.FromObject(value));
        }

        public IJsonToken? Read(string json)
        {
            return JsonToken.Parse(json);
        }
    }
}
