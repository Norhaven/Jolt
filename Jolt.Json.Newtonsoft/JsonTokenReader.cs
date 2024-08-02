using Jolt.Exceptions;
using Jolt.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Json.Newtonsoft
{
    public sealed class JsonTokenReader : IJsonTokenReader
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
            else if (value is IEnumerable<IGrouping<string?, IJsonToken>> grouping)
            {
                var grouped = new List<JToken>();

                foreach(var group in grouping)
                {
                    var json = new JObject
                    { 
                        ["key"] = group.Key.ToString(),
                        ["results"] = new JArray(group.Select(x => JToken.Parse(x.ToString())))
                    };

                    grouped.Add(json);
                }

                return JsonToken.FromObject(JToken.FromObject(grouped));
            }
            else if (value is IEnumerable<IJsonToken> sequence)
            {
                return JsonToken.FromObject(JToken.FromObject(sequence.Select(x => JToken.Parse(x.ToString()))));
            }
            
            return JsonToken.FromObject(JToken.FromObject(value));
        }

        public IJsonToken? Read(string json)
        {
            return JsonToken.Parse(json);
        }
    }
}
