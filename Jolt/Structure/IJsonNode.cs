using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Structure
{
    public interface IJsonNode : IEnumerable<IJsonNode>
    {
        IJsonNode this[string propertyName] { get; }
        IJsonNode this[int index] { get; }

        IJsonNode? Parent { get; }
        string? PropertyName { get; }
        object? Value { get; set; }
        JsonNodeType Type { get; }
        bool IsProperty { get; }
        bool IsValue { get; }

        IJsonNode? SelectNodeAtPath(string path);
        IJsonNode? Copy(string? propertyName = default);
        IJsonNode? ApplyChange(object? value);
        IJsonNode? ApplyChange(string propertyName, object? value);
        IJsonNode? ApplyChange(IJsonNode? node);
        IJsonNode? GetChildTemplate();
        string ToString();
    }
}
