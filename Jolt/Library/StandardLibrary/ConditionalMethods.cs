using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library.StandardLibrary
{
    [IncludeInStandardLibrary]
    internal sealed class ConditionalMethods
    {
        [JoltLibraryMethod("if")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName | LibraryMethodTarget.PropertyValue)]
        public static EvaluationResult? If(object? result, [LazyEvaluation] Expression trueExpression, [LazyEvaluation] Expression falseExpression, EvaluationContext context)
        {
            var resolved = result is RangeVariable variable ? variable.Value?.ToTypeOf<bool?>() : context.ResolveQueryPathIfPresent(result);

            var isTrue = resolved switch
            {
                bool value => value,
                IJsonToken token => token.Type == JsonTokenType.Value && token.AsValue().ValueType == JsonValueType.Boolean && token.AsValue().ToTypeOf<bool>(),
                _ => false
            };

            var expression = isTrue ? trueExpression : falseExpression;

            var evaluationContext = new EvaluationContext(
                context.Mode,
                expression,
                context.JsonContext,
                context.Token,
                context.Scope,
                context.Transform
            );

            return context.JsonContext.ExpressionEvaluator.Evaluate(evaluationContext);
        }

        [JoltLibraryMethod("includeIf", true)]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName)]
        public static IJsonToken? IncludeIf(object? result, EvaluationContext context)
        {
            var resolvedToken = context.ResolveQueryPathIfPresent(result);

            var shouldInclude = resolvedToken switch
            {
                bool include => include,
                IJsonValue value when value.IsBoolean() => value.ToTypeOf<bool>(),
                _ => throw context.CreateExecutionErrorFor<ConditionalMethods>(ExceptionCode.UnableToCompleteIncludeIfLibraryCallDueToNonBooleanCondition, result)
            };

            if (!shouldInclude)
            {
                return default;
            }

            var includedJson = context.Token.CurrentTransformerToken;
            var propertyName = context.Token.ResolvedPropertyName ?? context.Token.PropertyName;
            var currentSource = new SourceToken(0, default);

            var evaluationToken = new EvaluationToken(propertyName, default, includedJson.Parent, includedJson, currentSource);

            return context.Transform(evaluationToken, context.Scope);
        }
    }
}
