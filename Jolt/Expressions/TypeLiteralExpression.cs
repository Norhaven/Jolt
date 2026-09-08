using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    internal sealed class TypeLiteralExpression : Expression
    {
        public string TypeName;

        public TypeLiteralExpression(string typeName)
        {
            TypeName = typeName;
        }
    }
}
