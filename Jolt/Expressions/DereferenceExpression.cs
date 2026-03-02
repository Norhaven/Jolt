using Jolt.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public sealed class DereferenceExpression : Expression
    {
        public string PropertyName { get; }
        public bool IsNullSafe { get; }

        public DereferenceExpression(string propertyName, bool isNullSafe)
        {
            PropertyName = propertyName;
            IsNullSafe = isNullSafe;
        }
    }
}
