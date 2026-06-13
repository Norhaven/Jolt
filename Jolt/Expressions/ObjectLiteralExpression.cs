using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Expressions
{
    public sealed class ObjectLiteralExpression : Expression
    {
        public ObjectLiteralPropertyExpression[] Properties { get; }

        public ObjectLiteralExpression(IEnumerable<ObjectLiteralPropertyExpression> properties)
        {
            Properties = properties.ToArray();
        }
    }
}
