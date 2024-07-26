using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing
{
    public class MethodSignature
    {
        public string Assembly { get; }
        public string TypeName { get; }
        public string Name { get; }
        public string Alias { get; }
        public string FullyQualifiedName => $"{Assembly},{TypeName}.{Alias}";
        public MethodParameter[] Parameters { get; } = Array.Empty<MethodParameter>();
        public Type ReturnType { get; }
        public CallType CallType { get; }
        public bool IsSystemMethod { get; }
        public bool IsValueGenerator { get; }

        public MethodSignature(string assembly, string typeName, string name, string alias, Type returnType, CallType callType, bool isSystemMethod, bool isValueGenerator, params MethodParameter[] parameters)
        {
            Assembly = assembly;
            TypeName = typeName;
            Name = name;
            Alias = alias ?? name;
            ReturnType = returnType;
            CallType = callType;
            Parameters = parameters;
            IsSystemMethod = isSystemMethod;
            IsValueGenerator = isValueGenerator;
        }
    }
}
