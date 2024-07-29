using Jolt.Exceptions;
using Jolt.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    internal static class OperatorEvaluator
    {
        public static T ConvertTo<T>(this object? instance) => (T)Convert.ChangeType(instance, typeof(T));

        public static bool IsSameTypeAs(this object? left, object? right) => (left is null && right is null) || left.GetType() == right.GetType();
        
        public static bool AreBothTypeOf<T>(object? left, object? right)
        {
            if (left is null || right is null)
            {
                return false;
            }

            return left is T && right is T;
        }

        public static bool CanBothConvertTo<T>(object? left, object? right)
        {
            if (!typeof(T).IsValueType && left is null && right is null)
            {
                return true;
            }

            // We're explicitly trying to convert here instead of using IsAssignableFrom() because
            // that won't work for differing primitive numeric types.

            try
            {
                var convertedLeft = Convert.ChangeType(left, typeof(T));
                var convertedRight = Convert.ChangeType(right, typeof(T));

                return true;
            }
            catch
            {
                return false;
            }
        }
                
        public static object? Evaluate(INumeric left, Operator @operator, INumeric right)
        {
            if (left is null || right is null)
            {
                throw new JoltExecutionException($"Unable to evaluate numeric operator expression using a null value");
            }
            
            return @operator switch
            {
                Operator.Equal => left.Equals(right),
                Operator.GreaterThan => left.IsGreaterThan(right),
                Operator.LessThan => left.IsLessThan(right),
                Operator.GreaterThanOrEquals => left.IsGreaterThan(right) || left.Equals(right),
                Operator.LessThanOrEquals => left.IsLessThan(right) || left.Equals(right),
                Operator.Addition => left.Add(right),
                Operator.Subtraction => left.Subtract(right),
                Operator.Multiplication => left.Multiply(right),
                Operator.Division => left.Divide(right),
                _ => default
            };
        }
    }
}
