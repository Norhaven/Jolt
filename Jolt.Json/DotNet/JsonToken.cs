using Jolt.Structure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using Node = System.Text.Json.Nodes.JsonNode;
using NodeType = System.Text.Json.JsonValueKind;

namespace Jolt.Json.DotNet
{
    public sealed class JsonToken 
    {
        private Node _node;

        public JsonToken? Parent => new JsonToken(_node.Parent);

        public string? PropertyName => _node?.GetPropertyName();

        internal Node UnderlyingNode => _node;

        public object? Value 
        {
            get => _node?.AsValue()?.GetValue<object>();
            set
            {
                if (_node is null)
                {
                    return;
                }

                _node[PropertyName] = JsonValue.Create(value);
            }
        }

        public JsonTokenType Type
        {
            get
            {
                return _node.GetValueKind() switch
                {
                    NodeType.Array => JsonTokenType.Array,
                    NodeType.Object => JsonTokenType.Object,
                    //NodeType.String => JsonTokenType.String,
                    //NodeType.Number => JsonTokenType.Number,
                    //NodeType.True => JsonTokenType.Boolean,
                    //NodeType.False => JsonTokenType.Boolean,
                    _ => JsonTokenType.Unknown
                };
            }
        }

        public bool IsProperty => throw new NotImplementedException();

        public bool IsValue => throw new NotImplementedException();

        public JsonToken this[int index] => throw new NotImplementedException();

        public JsonToken this[string propertyName] => throw new NotImplementedException();

        public JsonToken(Node? node)
        {
            _node = node;
        }

        public JsonToken? GetChildTemplate()
        {
            if (_node is null)
            {
                return default;
            }

            if (Type == JsonTokenType.Array)
            {
                var array = _node.AsArray();
                var template = array[0];
                var jsonNode = new JsonToken(template);

                array.Remove(template);

                return jsonNode;
            }

            return default;
        }

        public void AddChild(JsonToken? node)
        {
            if (node is null)
            {
                return;
            }

            if (Type == JsonTokenType.Array)
            {
                var array = _node.AsArray();

                if (node is JsonToken json)
                {
                    array.Add(json.UnderlyingNode);
                }
            }
        }

        public JsonToken? Copy(string? propertyName = default)
        {
            if (_node is null)
            {
                return default;
            }

            return null;
            //var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(_node);

            //if (!string.IsNullOrWhiteSpace(propertyName))
            //{
            //    keyValuePairs.Remove(_node.GetPropertyName());
            //    keyValuePairs[propertyName] = Value;
            //}

            //return new JsonToken(Node.Parse(JsonSerializer.Serialize(keyValuePairs)));
        }

        public IEnumerator<JsonToken> GetEnumerator() => CreateEnumerator();

        public JsonToken SelectNodeAtPath(string path) => throw new NotImplementedException();

        public override string ToString()
        {
            return _node.ToJsonString();
        }

        //IEnumerator IEnumerable.GetEnumerator() => CreateEnumerator();

        private IEnumerator<JsonToken> CreateEnumerator()
        {
            if (Type == JsonTokenType.Array)
            {
                return _node.AsArray().Select(x => new JsonToken(x)).GetEnumerator();
            }
            else if (Type == JsonTokenType.Object)
            {
                return _node.AsObject().Select(x => new JsonToken(x.Value)).GetEnumerator();
            }

            return Enumerable.Empty<JsonToken>().GetEnumerator();
        }

        public JsonToken? ApplyChange(object? value)
        {
            throw new NotImplementedException();
        }

        public JsonToken? ApplyChange(string propertyName, object? value)
        {
            throw new NotImplementedException();
        }

        public JsonToken? ApplyChange(JsonToken? node)
        {
            throw new NotImplementedException();
        }
    }
}
