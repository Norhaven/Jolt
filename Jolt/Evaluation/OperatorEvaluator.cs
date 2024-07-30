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
    }
}
