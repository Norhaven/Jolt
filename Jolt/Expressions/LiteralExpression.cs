using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public class LiteralExpression : Expression
    {
        public Type Type { get; }
        public string Value { get; }

        public LiteralExpression(Type type, string value)
        {
            Type = type;
            Value = value;
        }
    }
}
