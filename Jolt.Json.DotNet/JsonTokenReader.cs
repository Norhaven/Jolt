using Jolt.Exceptions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Nodes = System.Text.Json.Nodes;

namespace Jolt.Json.DotNet
{
    public sealed class JsonTokenReader : IJsonTokenReader
    {
        public IJsonToken? CreateArrayFrom(IEnumerable<IJsonToken> tokens)
        {
            var array = new Nodes.JsonArray();

            foreach (var token in tokens)
            {
                if (token is IJsonValue value)
                {
                    array.Add(value.AsObject<object>());
                }
                else
                {
                    array.Add(Nodes.JsonNode.Parse(token.ToString()));
                }
            }

            return JsonToken.FromObject(array);
        }

        public IJsonToken? CreateObjectFrom(IEnumerable<IJsonToken> tokens)
        {
            var obj = new Nodes.JsonObject();

            foreach(var token in tokens)
            {
                if (token is IJsonObject json)
                {
                    foreach (var property in json)
                    {
                        obj[property.PropertyName] = Nodes.JsonNode.Parse(property.Value.ToString());
                    }
                }
                else if (token is IJsonProperty property)
                {
                    obj[property.PropertyName] = Nodes.JsonNode.Parse(property.Value.ToString());
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
                return JsonToken.FromObject(Nodes.JsonValue.Create<string>(null));
            }
            else if (value is IJsonToken token)
            {
                return token;
            }
            else if (value is IEnumerable<IGrouping<string?, IJsonToken>> grouping)
            {
                var grouped = new List<Nodes.JsonNode>();

                foreach(var group in grouping)
                {
                    var json = new Nodes.JsonObject
                    { 
                        ["key"] = group.Key.ToString(),
                        ["results"] = new Nodes.JsonArray(group.Select(x => Nodes.JsonNode.Parse(x.ToString())).ToArray())
                    };

                    grouped.Add(json);
                }

                return JsonToken.FromObject(new Nodes.JsonArray(grouped.ToArray()));
            }
            else if (value is IEnumerable<IJsonToken> sequence)
            {
                return JsonToken.FromObject(new Nodes.JsonArray(sequence.Select(x => Nodes.JsonNode.Parse(x.ToString())).ToArray()));
            }
            
            return JsonToken.FromObject(JsonSerializer.SerializeToNode(value));
        }

        public IJsonToken? Read(string json)
        {
            return JsonToken.Parse(json);
        }
    }
}
