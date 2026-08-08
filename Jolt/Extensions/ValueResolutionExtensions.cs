using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Library.StandardLibrary;
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
            return ResolveValueOf(context, value, typeof(T));
        }

        public static object? ResolveValueOf(this EvaluationContext context, object? value, Type type)
        {
            return value switch
            {
                RangeVariable variable => variable.Value?.ToTypeOf(type),
                DereferencedPath path when path.MissingPaths.Length == 0 => path.ObtainableToken,
                DereferencedPath path when path.MissingPaths.Length > 0 => throw context.CreateExecutionErrorFor<LoopAndQueryMethods>(ExceptionCode.UnableToPerformLibraryCallOnMissingPath, path.MissingPaths[0]),
                _ => context.ResolveQueryPathIfPresent(value)
            };
        }
    }
}
