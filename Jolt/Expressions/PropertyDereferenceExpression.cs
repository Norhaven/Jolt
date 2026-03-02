using Jolt.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public sealed class PropertyDereferenceExpression : Expression
    {
        public RangeVariableExpression Variable { get; }
        public DereferenceExpression[] DereferenceChain { get; }

        public PropertyDereferenceExpression(RangeVariableExpression variable, DereferenceExpression[] dereferenceChain)
        {
            Variable = variable;
            DereferenceChain = dereferenceChain;
        }
    }
}
