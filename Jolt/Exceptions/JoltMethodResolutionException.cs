using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Exceptions
{
    public class JoltMethodResolutionException : JoltException
    {
        public string TypeName { get; }
        public string MethodName { get; }

        public JoltMethodResolutionException(ExceptionCode code, string typeName, string methodName, string message) 
            : base(code, message)
        {
            TypeName = typeName;
            MethodName = methodName;
        }
    }
}
