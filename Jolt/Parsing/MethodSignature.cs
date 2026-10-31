using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Jolt.Parsing
{
    [DebuggerDisplay("{Name}({Parameters.Count})")]
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
        public bool IsAllowedAsMatchCase { get; }
        public bool IsUnsafe { get; }
        public Delegate? MethodDelegate { get; }

        public MethodSignature(string assemblyQualifiedTypeName, string name, string alias, Type returnType, CallType callType, bool isSystemMethod, bool isValueGenerator, bool isAllowedAsPropertyName, bool isAllowedAsPropertyValue, bool isAllowedAsStatement, bool isAllowedAsMatchCase, bool isUnsafe, params MethodParameter[] parameters)
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
            IsAllowedAsMatchCase = isAllowedAsMatchCase;
            IsUnsafe = isUnsafe;
        }

        /// <summary>
        /// Initializes a signature for an external method that invokes a delegate. As with every external method, it is
        /// only valid in a property value.
        /// </summary>
        public MethodSignature(string name, Delegate method, Type returnType, params MethodParameter[] parameters)
            : this(string.Empty, name, name, returnType, CallType.Delegate, false, false, false, true, false, false, false, parameters)
        {
            MethodDelegate = method;
        }
    }
}
