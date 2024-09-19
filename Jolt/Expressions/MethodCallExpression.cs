using Jolt.Evaluation;
using Jolt.Parsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Jolt.Expressions
{
    [DebuggerDisplay("{Signature.Alias}({ParameterValues.Length})")]
    public sealed class MethodCallExpression : Expression
    {
        public MethodSignature Signature { get; }
        public Expression[] ParameterValues { get; }
        public string? GeneratedName { get; }
        public RangeVariable GeneratedVariable { get; }

        public MethodCallExpression(MethodSignature signature, Expression[] parameterValues, string? generatedName = null, RangeVariable generatedVariable = default)
        {
            Signature = signature;
            ParameterValues = parameterValues;
            GeneratedName = generatedName;
            GeneratedVariable = generatedVariable;
        }

        public MethodCallExpression WithParameters(IEnumerable<Expression> parameters)
        {
            return new MethodCallExpression(Signature, parameters.ToArray(), GeneratedName, GeneratedVariable);
        }
    }
}
