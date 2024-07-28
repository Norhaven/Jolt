using Jolt.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library
{
    public sealed class MethodRegistration
    {
        public static MethodRegistration FromStaticMethod<T>(string methodName, string alias = default) => FromStaticMethod(typeof(T), methodName, alias);
        public static MethodRegistration FromStaticMethod(Type type, string methodName, string alias = default) => new MethodRegistration(type.AssemblyQualifiedName, methodName, alias);
        public static MethodRegistration FromInstanceMethod(string methodName, string alias = default) => new MethodRegistration(methodName, alias);

        public string FullyQualifiedTypeName { get; }
        public string MethodName { get; }
        public CallType CallType { get; }
        public string Alias { get; }

        public MethodRegistration(string assemblyQualifiedTypeName, string staticMethodName, string alias)
        {
            FullyQualifiedTypeName = assemblyQualifiedTypeName;
            MethodName = staticMethodName;
            CallType = CallType.Static;
            Alias = alias;
        }

        public MethodRegistration(string instanceMethodName, string alias)
        {
            FullyQualifiedTypeName = string.Empty;
            MethodName = instanceMethodName;
            Alias = alias;
            CallType = CallType.Instance;
        }
    }
}
