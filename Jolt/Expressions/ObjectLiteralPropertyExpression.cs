using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public sealed class ObjectLiteralPropertyExpression : Expression
    {
        public string PropertyName { get; }
        public Expression PropertyValue { get; }

        public ObjectLiteralPropertyExpression(string propertyName, Expression propertyValue)
        {
            PropertyName = propertyName;
            PropertyValue = propertyValue;
        }
    }
}
