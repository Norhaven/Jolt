using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Library
{
    public static class StandardLibraryMethods
    {
        [JoltLibraryMethod("valueOf")]
        public static IJsonToken? ValueOf(string path, EvaluationContext context)
        {
            // ValueOf will always start a search from the root, other similar methods may search differently.

            return context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.ClosureSources, path, JsonQueryMode.StartFromRoot);
        }

        [JoltLibraryMethod("exists")]
        public static IJsonToken? Exists(object? pathOrValue, EvaluationContext context)
        {
            var valueExists = pathOrValue != null;

            if (pathOrValue is IJsonToken token)
            {
                var tokenExists = valueExists && token.Type != JsonTokenType.Null && token.AsValue().AsObject<object>() != null;

                return context.JsonContext.JsonTokenReader.CreateTokenFrom(tokenExists);
            }

            if (pathOrValue is string path)
            {
                var tokenValue = context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.ClosureSources, path, JsonQueryMode.StartFromRoot);

                var tokenExists = tokenValue != null && tokenValue.Type != JsonTokenType.Null;

                return context.JsonContext.JsonTokenReader.CreateTokenFrom(tokenExists);
            }

            return context.JsonContext.JsonTokenReader.CreateTokenFrom(valueExists);
        }

        [JoltLibraryMethod("if")]
        public static EvaluationResult? If(object? result, [LazyEvaluation]Expression trueExpression, [LazyEvaluation]Expression falseExpression, EvaluationContext context)
        {
            var isTrue = result switch
            {
                bool value => value,
                IJsonToken token => token.Type == JsonTokenType.Value && token.AsValue().ValueType == JsonValueType.Boolean && token.AsValue().ToTypeOf<bool>(),
                null => false
            };

            var expression = isTrue ? trueExpression : falseExpression;

            var evaluationContext = new EvaluationContext(
                context.Mode,
                expression,
                context.JsonContext,
                context.Token,
                context.ClosureSources,
                context.Transform
            );

            return context.JsonContext.ExpressionEvaluator.Evaluate(evaluationContext);
        }

        [JoltLibraryMethod("eval")]
        public static IJsonToken? Evaluate(object? result, EvaluationContext context)
        {
            return context.JsonContext.JsonTokenReader.CreateTokenFrom(result);
        }

        [JoltLibraryMethod("loop")]
        public static IEnumerable<IJsonToken> LoopOnArrayAtPath(string path, EvaluationContext context)
        {
            if (context.Token.CurrentTransformerToken.Type != JsonTokenType.Array)
            {
                yield break;
            }

            var closestViableNode = context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.ClosureSources, path, JsonQueryMode.StartFromClosestMatch);

            if (closestViableNode?.Type != JsonTokenType.Array)
            {
                yield break;
            }

            var transformerArray = context.Token.CurrentTransformerToken.AsArray();

            var contentTemplate = transformerArray.RemoveAt(0);

            foreach (var propertyOrElement in closestViableNode.AsArray() ?? Enumerable.Empty<IJsonToken>())
            {
                var templateCopy = contentTemplate.Copy();
                var propertyName = context.Token.ResolvedPropertyName ?? context.Token.PropertyName;

                var templateEvaluationToken = new EvaluationToken(propertyName, default, transformerArray, templateCopy);

                context.ClosureSources.Push(propertyOrElement);

                yield return context.Transform(templateEvaluationToken, context.ClosureSources);
                
                context.ClosureSources.Pop();
            }
        }

        [JoltLibraryMethod("loopValueOf")]
        public static IJsonToken LoopValueOf(string path, EvaluationContext context)
        {
            // LoopValueOf will always search from the closest node, other similar methods may search differently.

            return context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.ClosureSources, path, JsonQueryMode.StartFromClosestMatch);
        }

        [JoltLibraryMethod("loopValue")]
        public static IJsonToken? LoopValue(EvaluationContext context)
        {
            return context.ClosureSources.Peek();
        }

        [JoltLibraryMethod("toInt")]
        public static IJsonToken? ToInt(object? value, EvaluationContext context)
        {
            return ConvertToType<int>(value, context);
        }

        [JoltLibraryMethod("toString")]
        public static IJsonToken? ToString(object? value, EvaluationContext context)
        {
            var convertedValue = value?.ToString();
            
            return context.JsonContext.JsonTokenReader.CreateTokenFrom(convertedValue);
        }

        [JoltLibraryMethod("toDecimal")]
        public static IJsonToken? ToDecimal(object? value, EvaluationContext context)
        {
            return ConvertToType<double>(value, context);
        }

        [JoltLibraryMethod("toBool")]
        public static IJsonToken? ToBool(object? value, EvaluationContext context)
        {
            return ConvertToType<bool>(value, context);
        }

        private static IJsonToken? ConvertToType<T>(object? value, EvaluationContext context)
        {
            if (value is null || value is T)
            {
                return context.JsonContext.JsonTokenReader.CreateTokenFrom(value);
            }

            var convertedValue = Convert.ChangeType(value, typeof(T));

            return context.JsonContext.JsonTokenReader.CreateTokenFrom(convertedValue);
        }
    }
}
