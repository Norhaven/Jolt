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
        /// <summary>
        /// Resolves a library method argument to its value. A null value, or a path that resolves to nothing, is treated
        /// as an error unless it was reached through a null-safe ('?.') dereference, in which case null is returned.
        /// </summary>
        public static object? ResolveValueOf<T>(this EvaluationContext context, object? value) where T : class
        {
            return ResolveValueOf(context, value, typeof(T));
        }

        /// <inheritdoc cref="ResolveValueOf{T}(EvaluationContext, object?)"/>
        public static object? ResolveValueOf(this EvaluationContext context, object? value, Type type)
        {
            return Resolve(context, value, type, throwOnNullOrMissing: true);
        }

        /// <summary>
        /// Resolves a library method argument to its value, returning null instead of throwing when the value is null
        /// or the path resolves to nothing. This is intended for methods whose purpose is to test the value itself.
        /// </summary>
        public static object? ResolveValueOrNullOf<T>(this EvaluationContext context, object? value) where T : class
        {
            return Resolve(context, value, typeof(T), throwOnNullOrMissing: false);
        }

        private static object? Resolve(EvaluationContext context, object? value, Type type, bool throwOnNullOrMissing)
        {
            object? NullValue() => throwOnNullOrMissing ? throw context.CreateExecutionErrorFor<LoopAndQueryMethods>(ExceptionCode.UnableToPerformLibraryCallOnNullValue) : default(object);
            object? MissingPath(string path) => throwOnNullOrMissing ? throw context.CreateExecutionErrorFor<LoopAndQueryMethods>(ExceptionCode.UnableToPerformLibraryCallOnMissingPath, path) : default(object);

            return value switch
            {
                null => NullValue(),
                RangeVariable variable when IsNull(variable.Value) => NullValue(),
                RangeVariable variable => variable.Value?.ToTypeOf(type),
                DereferencedPath path when path.MissingPaths.Length > 0 && path.IsNullSafe => default,
                DereferencedPath path when path.MissingPaths.Length > 0 => MissingPath(path.MissingPaths[0]),
                DereferencedPath path when IsNull(path.ObtainableToken) => NullValue(),
                DereferencedPath path => path.ObtainableToken,
                string path when context.IsQueryPath(path) => context.ResolveQueryPathIfPresent(path) switch
                {
                    null => MissingPath(path),
                    IJsonToken token when IsNull(token) => NullValue(),
                    var token => token
                },
                _ => value
            };
        }

        private static bool IsNull(IJsonToken? token)
        {
            return token is null || token.Type == JsonTokenType.Null || (token is IJsonValue value && value.ValueType == JsonValueType.Null);
        }
    }
}
