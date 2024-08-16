using Jolt.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public sealed class PropertyDereferenceExpression : Expression
    {
        public RangeVariableExpression Variable { get; }
        public string[] PropertyPaths { get; }

        public PropertyDereferenceExpression(RangeVariableExpression variable, string[] propertyPaths)
        {
            Variable = variable;
            PropertyPaths = propertyPaths;
        }
    }
}
