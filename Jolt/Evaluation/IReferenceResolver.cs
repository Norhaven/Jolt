using Jolt.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public interface IReferenceResolver
    {
        MethodSignature? GetMethod(string methodName);
    }
}
