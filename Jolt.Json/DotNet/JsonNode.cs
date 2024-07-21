using Jolt.Structure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Node = System.Text.Json.Nodes.JsonNode;
using NodeType = System.Text.Json.JsonValueKind;

namespace Jolt.Json.DotNet
{
    public sealed class JsonNode : IJsonNode
    {
        private Node _node;

        public IJsonNode? Parent => new JsonNode(_node.Parent);

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

        public JsonNodeType Type
        {
            get
            {
                return _node.GetValueKind() switch
                {
                    NodeType.Array => JsonNodeType.Array,
                    NodeType.Object => JsonNodeType.Object,
                    NodeType.String => JsonNodeType.String,
                    NodeType.Number => JsonNodeType.Number,
                    NodeType.True => JsonNodeType.Boolean,
                    NodeType.False => JsonNodeType.Boolean,
                    _ => JsonNodeType.Unknown
                };
            }
        }

        public bool IsProperty => throw new NotImplementedException();

        public bool IsValue => throw new NotImplementedException();

        public IJsonNode this[int index] => throw new NotImplementedException();

        public IJsonNode this[string propertyName] => throw new NotImplementedException();

        public JsonNode(Node? node)
        {
            _node = node;
        }

        public IJsonNode? GetChildTemplate()
        {
            if (_node is null)
            {
                return default;
            }

            if (Type == JsonNodeType.Array)
            {
                var array = _node.AsArray();
                var template = array[0];
                var jsonNode = new JsonNode(template);

                array.Remove(template);

                return jsonNode;
            }

            return default;
        }

        public void AddChild(IJsonNode? node)
        {
            if (node is null)
            {
                return;
            }

            if (Type == JsonNodeType.Array)
            {
                var array = _node.AsArray();

                if (node is JsonNode json)
                {
                    array.Add(json.UnderlyingNode);
                }
            }
        }

        public IJsonNode? Copy(string? propertyName = default)
        {
            if (_node is null)
            {
                return default;
            }

            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(_node);

            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                keyValuePairs.Remove(_node.GetPropertyName());
                keyValuePairs[propertyName] = Value;
            }

            return new JsonNode(Node.Parse(JsonSerializer.Serialize(keyValuePairs)));
        }

        public IEnumerator<IJsonNode> GetEnumerator() => CreateEnumerator();

        public IJsonNode SelectNodeAtPath(string path) => throw new NotImplementedException();

        public override string ToString()
        {
            return _node.ToJsonString();
        }

        IEnumerator IEnumerable.GetEnumerator() => CreateEnumerator();

        private IEnumerator<IJsonNode> CreateEnumerator()
        {
            if (Type == JsonNodeType.Array)
            {
                return _node.AsArray().Select(x => new JsonNode(x)).GetEnumerator();
            }
            else if (Type == JsonNodeType.Object)
            {
                return _node.AsObject().Select(x => new JsonNode(x.Value)).GetEnumerator();
            }

            return Enumerable.Empty<IJsonNode>().GetEnumerator();
        }

        public IJsonNode? ApplyChange(object? value)
        {
            throw new NotImplementedException();
        }

        public IJsonNode? ApplyChange(string propertyName, object? value)
        {
            throw new NotImplementedException();
        }

        public IJsonNode? ApplyChange(IJsonNode? node)
        {
            throw new NotImplementedException();
        }
    }
}
