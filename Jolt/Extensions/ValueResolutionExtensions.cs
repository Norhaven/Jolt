using Jolt.Evaluation;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Extensions
{
    internal static class ValueResolutionExtensions
    {
        public static object? ResolveValueOf<T>(this EvaluationContext context, object? value) where T : class
        {
            return value is RangeVariable variable ? variable.Value?.ToTypeOf<T>() : context.ResolveQueryPathIfPresent(value);
        }

        public static object? ResolveValueOf(this EvaluationContext context, object? value, Type type)
        {
            return value is RangeVariable variable ? variable.Value?.ToTypeOf(type) : context.ResolveQueryPathIfPresent(value);
        }
    }
}
