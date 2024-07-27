using Jolt.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library
{
    public class MethodRegistration
    {
        public string FullyQualifiedTypeName { get; }
        public string MethodName { get; }
        public CallType CallType { get; }
        public bool IsStandardLibraryCall { get; }

        public MethodRegistration(string assemblyQualifiedTypeName, string staticMethodName)
        {
            FullyQualifiedTypeName = assemblyQualifiedTypeName;
            MethodName = staticMethodName;
            CallType = CallType.Static;
        }

        internal MethodRegistration(string fullyQualifiedTypeName, string staticMethodName, bool isStandardLibraryCall)
        {
            FullyQualifiedTypeName = fullyQualifiedTypeName;
            MethodName = staticMethodName;
            CallType = CallType.Static;
        }

        public MethodRegistration(string instanceMethodName)
        {
            FullyQualifiedTypeName = string.Empty;
            MethodName = instanceMethodName;
            CallType = CallType.Instance;
        }
    }
}
