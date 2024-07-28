using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    public class JoltMethodResolutionException : JoltException
    {
        public string TypeName { get; }
        public string MethodName { get; }

        public JoltMethodResolutionException(string typeName, string methodName, string message) 
            : base(message)
        {
            TypeName = typeName;
            MethodName = methodName;
        }
    }
}
