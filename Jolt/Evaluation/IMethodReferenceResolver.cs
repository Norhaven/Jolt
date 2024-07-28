using Jolt.Library;
using Jolt.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public interface IMethodReferenceResolver
    {
        MethodSignature? GetMethod(string methodName);
        void RegisterMethods(IEnumerable<MethodRegistration> methodRegistrations, object? methodContext = default);
        void Clear();
    }
}
