using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library
{
    /// <summary>
    /// Represents a marker that indicates that a parameter is lazily evaluated. This parameter must be
    /// of type <see cref="Expressions.Expression"/> due to the need to possibly evaluate the expression at runtime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class LazyEvaluationAttribute : Attribute
    {
    }
}
