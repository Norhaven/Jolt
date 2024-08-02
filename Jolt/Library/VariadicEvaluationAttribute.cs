using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library
{
    /// <summary>
    /// Represents a marker that indicates that a parameter is variadic in nature. This must be on the final
    /// method parameter (aside from the EvaluationContext parameter that ends all library calls).
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class VariadicEvaluationAttribute : Attribute
    {
    }
}
