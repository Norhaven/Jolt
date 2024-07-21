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

        public MethodRegistration(string fullyQualifiedTypeName, string methodName)
        {
            FullyQualifiedTypeName = fullyQualifiedTypeName;
            MethodName = methodName;
            CallType = CallType.Static;
        }

        internal MethodRegistration(string fullyQualifiedTypeName, string methodName, bool isStandardLibraryCall)
        {
            FullyQualifiedTypeName = fullyQualifiedTypeName;
            MethodName = methodName;
            CallType = CallType.Static;
        }

        public MethodRegistration(string methodName)
        {
            FullyQualifiedTypeName = string.Empty;
            MethodName = methodName;
            CallType = CallType.Instance;
        }
    }
}
