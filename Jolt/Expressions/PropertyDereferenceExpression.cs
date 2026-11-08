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

        /// <summary>
        /// Gets the indexes or slices applied in turn to the variable itself before its chain of properties (e.g. the "[0]" in "@x[0].name").
        /// </summary>
        public RangeExpression[] VariableIndexers { get; }

        public PropertyDereferenceExpression(RangeVariableExpression variable, DereferenceExpression[] dereferenceChain, RangeExpression[]? variableIndexers = default)
        {
            Variable = variable;
            DereferenceChain = dereferenceChain;
            VariableIndexers = variableIndexers ?? Array.Empty<RangeExpression>();
        }
    }
}
