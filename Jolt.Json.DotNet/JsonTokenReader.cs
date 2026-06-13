using Jolt.Evaluation;
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
        public IJsonToken? CreateArrayFrom(IEnumerable<IJsonToken>? tokens)
        {
            var array = new Nodes.JsonArray();

            foreach (var token in tokens ?? Enumerable.Empty<IJsonToken>())
            {
                if (token is IJsonValue value)
                {
                    array.Add(value.ToTypeOf<object>());
                }
                else if (token is null)
                {
                    array.Add(Nodes.JsonValue.Create<string>(null));
                }
                else
                {
                    array.Add(Nodes.JsonNode.Parse(token.ToString()));
                }
            }

            return JsonToken.FromObject(array);
        }

        public IJsonToken? CreateObjectFrom(IEnumerable<IJsonToken>? tokens)
        {
            var obj = new Nodes.JsonObject();

            foreach(var token in tokens ?? Enumerable.Empty<IJsonToken>())
            {
                if (token is IJsonObject json)
                {
                    foreach (var property in json)
                    {
                        if (property.Value is JsonToken propertyToken)
                        {
                            obj[property.PropertyName] = propertyToken.UnderlyingNode.DeepClone();
                        }
                    }
                }
                else if (token is IJsonProperty property)
                {
                    obj[property.PropertyName] = Nodes.JsonNode.Parse(property.Value.ToString());
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(token), $"Unable to create JSON object from unsupported token type '{token.Type}'");
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
            else if (value is IEnumerable<IGrouping<object, IJsonToken>> grouping)
            {
                var grouped = new List<Nodes.JsonNode>();

                Nodes.JsonNode? ReadOrParse(IJsonToken token)
                {
                    if (token.Type == Structure.JsonTokenType.Value)
                    {
                        return Nodes.JsonValue.Create(token.AsValue().ToTypeOf<object>());
                    }
                    else
                    {
                        return Nodes.JsonNode.Parse(token.ToString());
                    }
                }

                foreach (var group in grouping)
                {
                    var json = new Nodes.JsonObject
                    { 
                        ["key"] = Nodes.JsonValue.Create(group.Key),
                        ["results"] = new Nodes.JsonArray(group.Select(ReadOrParse).ToArray())
                    };

                    grouped.Add(json);
                }

                return JsonToken.FromObject(new Nodes.JsonArray(grouped.ToArray()));
            }
            else if (value is IEnumerable<IJsonToken> sequence)
            {
                return CreateArrayFrom(sequence);
            }
            else if (value is double d)
            {
                return JsonValue.Parse(d.ToString());
            }
            else if (value is Dictionary<string, IJsonToken> dictionary)
            {
                var obj = new Nodes.JsonObject();

                foreach (var pair in dictionary)
                {
                    if (pair.Value is JsonToken propertyToken)
                    {
                        obj[pair.Key] = propertyToken.UnderlyingNode?.DeepClone();
                    }
                }

                return JsonToken.FromObject(obj);
            }
            else if (value is RangeVariable variable)
            {
                return variable.Value;
            }

            return JsonToken.FromObject(JsonSerializer.SerializeToNode(value));
        }

        public IJsonToken? Read(string json)
        {
            return JsonToken.Parse(json);
        }
    }
}
