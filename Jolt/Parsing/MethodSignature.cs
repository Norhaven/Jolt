using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing
{
    public sealed class MethodSignature
    {
        public string AssemblyQualifiedTypeName { get; }
        public string Name { get; }
        public string Alias { get; }
        public MethodParameter[] Parameters { get; } = Array.Empty<MethodParameter>();
        public Type ReturnType { get; }
        public CallType CallType { get; }
        public bool IsSystemMethod { get; }
        public bool IsValueGenerator { get; }
        public bool IsAllowedAsPropertyName { get; }
        public bool IsAllowedAsPropertyValue { get; }
        public bool IsAllowedAsStatement { get; }

        public MethodSignature(string assemblyQualifiedTypeName, string name, string alias, Type returnType, CallType callType, bool isSystemMethod, bool isValueGenerator, bool isAllowedAsPropertyName, bool isAllowedAsPropertyValue, bool isAllowedAsStatement, params MethodParameter[] parameters)
        {
            AssemblyQualifiedTypeName = assemblyQualifiedTypeName;
            Name = name;
            Alias = alias ?? name;
            ReturnType = returnType;
            CallType = callType;
            Parameters = parameters;
            IsSystemMethod = isSystemMethod;
            IsValueGenerator = isValueGenerator;
            IsAllowedAsPropertyName = isAllowedAsPropertyName;
            IsAllowedAsPropertyValue = isAllowedAsPropertyValue;
            IsAllowedAsStatement = isAllowedAsStatement;
        }
    }
}
