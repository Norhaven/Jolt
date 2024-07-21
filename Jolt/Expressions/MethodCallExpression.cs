using Jolt.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public class MethodCallExpression : Expression
    {
        public MethodSignature Signature { get; }
        public Expression[] ParameterValues { get; }
        public string? GeneratedName { get; }

        public MethodCallExpression(MethodSignature signature, Expression[] parameterValues, string? generatedName = null)
        {
            Signature = signature;
            ParameterValues = parameterValues;
            GeneratedName = generatedName;
        }
    }
}
