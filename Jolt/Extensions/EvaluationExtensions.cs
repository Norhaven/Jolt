using Jolt.Evaluation;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Extensions
{
    internal static class EvaluationExtensions
    {
        public static object? UnwrapWith<T>(this T value, IJsonTokenReader reader)
        {
            if (value is EvaluationResult result)
            {
                return result.TransformedToken.UnwrapWith(reader);
            }

            if (value is IJsonToken token)
            {
                return token.AsValue().AsObject<object>();
            }

            return value;
        }
    }
}
