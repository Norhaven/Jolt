using Jolt.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    internal sealed class Numeric : INumeric
    {
        private static readonly ISet<Type> _numericTypes = new HashSet<Type>
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)
        };

        public static bool IsSupported(object? value)
        {
            return value != null && _numericTypes.Contains(value.GetType());
        }

        private readonly IComparable _value;

        public Numeric(object? value) : this(value as IComparable) { }

        public Numeric(IComparable? value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "Unable to create a numeric type for a null value");
            }

            if (!_numericTypes.Contains(value.GetType()))
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Unable to create a numeric type for unsupported type '{value.GetType()}'");
            }

            _value = value;
        }

        public object? Add(object? value)
        {
            return PerformOperationWith(value, Operator.Addition, (left, right) => left + right, (left, right) => left + right);
        }

        public object? Divide(object? value)
        {
            return PerformOperationWith(value, Operator.Division, (left, right) => left / right, (left, right) => left / right);
        }

        public bool IsGreaterThan(object? value) => (bool)PerformOperationWith(value, Operator.GreaterThan, (left, right) => left.CompareTo(right) > 0, (left, right) => left.CompareTo(right) > 0);

        public bool IsLessThan(object? value) => (bool)PerformOperationWith(value, Operator.LessThan, (left, right) => left.CompareTo(right) < 0, (left, right) => left.CompareTo(right) < 0);

        public override bool Equals(object obj) => (bool)PerformOperationWith(obj, Operator.Equals, (left, right) => left == right, (left, right) => left == right);

        public override int GetHashCode() => _value.GetHashCode();

        public object? Multiply(object? value)
        {
            return PerformOperationWith(value, Operator.Multiplication, (left, right) => left * right, (left, right) => left * right);
        }

        public object? Subtract(object? value)
        {
            return PerformOperationWith(value, Operator.Subtraction, (left, right) => left - right, (left, right) => left - right);
        }

        private object? PerformOperationWith(object? value, Operator operation, Func<long, long, object> useIntegers, Func<double, double, object> useFloatingPoints)
        {
            value = UnwrapNumericIfPresent(value);
            
            // Handle the cases where we may have a non-whole number in the mix first, when that
            // happens we need to lift whatever the other one happens to be for the operation.
            // Internally we're assuming double or long for every numeric literal but the user may
            // also have additional types coming into the expressions from their external methods and
            // we need to handle those appropriately too. Note that we're currently not splitting out
            // the decimal operations from float/double so there may be a few slight rounding errors
            // using that type. We're also requiring that division use doubles in all cases because
            // when both operands are non-floating point types then the result will be truncated
            // and not be an accurate representation of the true value.

            if (_value is double || value is double || 
                _value is float || value is float ||
                _value is decimal || value is decimal ||
                operation == Operator.Division)
            {
                var result = useFloatingPoints(_value.ConvertTo<double>(), value.ConvertTo<double>());

                if (result is double d)
                {
                    // Depending on whether you use Newtonsoft or System.Text.Json it will represent the
                    // output in the result as a number in the resulting JSON with or without the decimal point
                    // and trailing zero. We need to ensure that the result is consistent for both cases
                    // so we're checking for a whole number and truncating if that's the case.

                    var isWholeNumber = Math.Abs(d % 1) <= (double.Epsilon * 100);

                    if (isWholeNumber)
                    {
                        return (long)d;
                    }
                }

                return result;
            }

            if (OperatorEvaluator.CanBothConvertTo<long>(_value, value))
            {
                return useIntegers(_value.ConvertTo<long>(), value.ConvertTo<long>());
            }

            throw new ArgumentOutOfRangeException($"Unable to perform numeric operation '{operation}' with arguments '{_value.GetType()}' and '{value.GetType()}'");
        }

        private object? UnwrapNumericIfPresent(object? value)
        {
            if (value is Numeric numeric)
            {
                return numeric._value;
            }

            return value;
        }
    }
}
