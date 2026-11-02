using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Library.StandardLibrary;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Jolt.Extensions
{
    internal static class ValueResolutionExtensions
    {
        /// <summary>
        /// Resolves a library method argument to its value. A null value, a missing property (or a chain skipped by a null-safe
        /// '?.' dereference, which gives null), or a JSON path that resolves to nothing is treated as an error.
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

        /// <summary>
        /// Resolves a library method argument that configures the call (e.g. a search string, delimiter, or count) rather than
        /// supplying the value that the method works on. A JSON path, variable, or dereferenced property is resolved to the value
        /// it holds, and that value (or any other, such as a literal) is converted to the given type. Null, a missing property, or a
        /// chain skipped by a null-safe ('?.') dereference gives the type's default value.
        /// </summary>
        public static T ResolveArgumentOf<T>(this EvaluationContext context, object? value)
        {
            var resolved = value switch
            {
                QueryPathReference path => context.SelectTokenAt(path) ?? throw context.CreateExecutionErrorFor<LoopAndQueryMethods>(ExceptionCode.UnableToPerformLibraryCallOnMissingPath, path.Path),
                RangeVariable variable => variable.Value,
                DereferencedPath path when path.MissingPaths.Length > 0 => default,
                DereferencedPath path => path.ObtainableToken,
                _ => value
            };

            if (resolved is IJsonToken token)
            {
                if (IsNull(token))
                {
                    return default!;
                }

                // Arrays and objects are kept as tokens, since there's no other value to convert them from.

                if (token.Type == JsonTokenType.Value)
                {
                    resolved = token.AsValue().ToTypeOf<object>();
                }
            }

            return resolved switch
            {
                null => default!,
                T typed => typed,
                _ => (T)Convert.ChangeType(resolved, typeof(T), CultureInfo.InvariantCulture)
            };
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
                DereferencedPath path when path.MissingPaths.Length > 0 || IsNull(path.ObtainableToken) => NullValue(),
                DereferencedPath path => path.ObtainableToken,
                QueryPathReference path => context.SelectTokenAt(path) switch
                {
                    null => MissingPath(path.Path),
                    IJsonToken token when IsNull(token) => NullValue(),
                    var token => token
                },
                _ => value
            };
        }

        internal static bool IsNull(IJsonToken? token)
        {
            return token is null || token.Type == JsonTokenType.Null || (token is IJsonValue value && value.ValueType == JsonValueType.Null);
        }
    }
}
