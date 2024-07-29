using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    public interface INumeric
    {
        object? Add(object? value);
        object? Subtract(object? value);
        object? Multiply(object? value);
        object? Divide(object? value);
        bool Equals(object? value);
        bool IsGreaterThan(object? value);
        bool IsLessThan(object? value);
    }
}
