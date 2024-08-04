using Jolt.Library;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing
{
    public sealed class MethodParameter
    {
        public Type Type { get; }
        public string Name { get; }
        public bool IsLazyEvaluated { get; }
        public bool IsVariadic { get; }
        public bool IsOptional { get; }
        public object? OptionalDefaultValue { get; }

        public MethodParameter(Type type, string name, bool isLazyEvaluated, bool isVariadic, bool isOptional, object? optionalDefaultValue)
        {
            Type = type;
            Name = name;
            IsLazyEvaluated = isLazyEvaluated;
            IsVariadic = isVariadic;
            IsOptional = isOptional;
            OptionalDefaultValue = optionalDefaultValue;
        }
    }
}
