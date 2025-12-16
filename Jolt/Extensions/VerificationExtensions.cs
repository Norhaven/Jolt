using Jolt.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Extensions
{
    internal static class VerificationExtensions
    {
        public static VerificationChainBuilder<T> ThrowIfNull<T>(this T? value, string parameterName, string? message = default)
            where T : class
        {
            if (value is null)
            {
                throw new ArgumentNullException(parameterName, message ?? "Expected value to be non-null but found null instead");
            }

            return new VerificationChainBuilder<T>(value);
        }

        public static VerificationChainBuilder<TResult> AndGet<T, TResult>(this VerificationChainBuilder<T> builder, Func<T, TResult> createResult)
            where T : class
            where TResult : class?
        {
            var retrievedValue = createResult(builder.Value);

            if (retrievedValue is null)
            {
                return new VerificationChainBuilder<TResult>(retrievedValue);
            }

            return new VerificationChainBuilder<TResult>(retrievedValue);
        }

        public static TResult AndFinally<T, TResult>(this VerificationChainBuilder<T> builder, Func<T, TResult> createResult)
            where T : class
        {            
            return createResult(builder.Value);
        }
    }
}
