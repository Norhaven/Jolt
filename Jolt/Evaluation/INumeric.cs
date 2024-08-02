using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    /// <summary>
    /// Represents a numeric object that can perform various math operations and comparisons.
    /// </summary>
    internal interface INumeric
    {
        /// <summary>
        /// Adds an object to this numeric value.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>An instance of <see cref="object"/> if successful, null otherwise.</returns>
        object? Add(object? value);

        /// <summary>
        /// Subtracts an object from this numeric value.
        /// </summary>
        /// <param name="value">The value to subtract.</param>
        /// <returns></returns>
        object? Subtract(object? value);

        /// <summary>
        /// Multiplies an object from the numeric value.
        /// </summary>
        /// <param name="value">The value to multiply.</param>
        /// <returns>An instance of <see cref="object"/> if successful, null otherwise.</returns>
        object? Multiply(object? value);

        /// <summary>
        /// Divides an object from the numeric value.
        /// </summary>
        /// <param name="value">The value to divide.</param>
        /// <returns>An instance of <see cref="object"/> if successful, null otherwise.</returns>
        object? Divide(object? value);

        /// <summary>
        /// Determines whether the numeric value is equal to the provided parameter.
        /// </summary>
        /// <param name="value">The value to compare.</param>
        /// <returns>True if the objects are equal, false otherwise.</returns>
        bool Equals(object? value);

        /// <summary>
        /// Determines whether the numeric value is greater than the provided parameter.
        /// </summary>
        /// <param name="value">The value to compare.</param>
        /// <returns>True if this is greater than the parameter, false otherwise.</returns>
        bool IsGreaterThan(object? value);

        /// <summary>
        /// Determines whether the numeric value is less than the provided parameter.
        /// </summary>
        /// <param name="value">The value to compare.</param>
        /// <returns>True if this is less than the parameter, false otherwise.</returns>
        bool IsLessThan(object? value);
    }
}
