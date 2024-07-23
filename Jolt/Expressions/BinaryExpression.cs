using Jolt.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Expressions
{
    public class BinaryExpression : Expression
    {
        public Expression Left { get; }
        public Expression Right { get; }
        public Operator Operator { get; }

        public bool IsComparison => Operator.IsAnyOf(
            Operator.Equal, 
            Operator.NotEqual, 
            Operator.GreaterThan, 
            Operator.LessThan, 
            Operator.GreaterThanOrEquals, 
            Operator.LessThanOrEquals);

        public BinaryExpression(Expression left, Operator @operator, Expression right)
        {
            Left = left;
            Operator = @operator;
            Right = right;
        }
    }
}
