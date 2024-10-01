using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            if (value is IJsonArray array)
            {
                return array;
            }

            if (value is IJsonObject obj)
            {
                return obj;
            }

            if (value is IJsonToken token)
            {
                return token.AsValue().ToTypeOf<object>();
            }

            return value;
        }

        public static IJsonToken? CreateTokenFrom(this EvaluationContext context, object? value)
        {
            return context.JsonContext.JsonTokenReader.CreateTokenFrom(value);
        }

        public static IJsonToken? CreateArrayFrom(this EvaluationContext context, IJsonToken[]? value)
        {
            return context.JsonContext.JsonTokenReader.CreateArrayFrom(value);
        }

        public static IJsonToken? CreateObjectFrom(this EvaluationContext context, IJsonToken[]? value)
        {
            return context.JsonContext.JsonTokenReader.CreateObjectFrom(value);
        }

        public static bool IsQueryPath(this EvaluationContext context, string path)
        {
            return context.JsonContext.QueryPathProvider.IsQueryPath(path);
        }

        public static object? ResolveQueryPathIfPresent(this EvaluationContext context, object? potentialPath)
        {
            if (potentialPath is null)
            {
                return default;
            }

            if (potentialPath is string value && context.IsQueryPath(value))
            {
                return context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.Scope.AvailableClosures, value, JsonQueryMode.StartFromRoot);
            }

            return potentialPath;
        }
        
        public static JoltException CreateExecutionErrorFor<T>(this EvaluationContext context, ExceptionCode exceptionCode, params object[] parameters)
        {
            return context.JsonContext.MessageProvider.CreateErrorFor<T>(MessageCategory.Execution, exceptionCode, parameters);
        }

        public static JoltException CreateExecutionErrorFor<T>(this EvaluationContext context, ExceptionCode exceptionCode, JoltException innerException, params object[] parameters)
        {
            return context.JsonContext.MessageProvider.CreateErrorFor<T>(MessageCategory.Execution, exceptionCode, innerException, parameters);
        }
    }
}
