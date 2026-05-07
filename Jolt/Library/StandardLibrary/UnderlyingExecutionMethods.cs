using Jolt.Evaluation;
using Jolt.Expressions;
using Jolt.Extensions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Library.StandardLibrary
{
    internal abstract class UnderlyingExecutionMethods
    {
        protected static IJsonToken? ExecuteLambdaBody(Expression lambdaBodyExpression, EvaluationContext context)
        {
            var evaluationContext = new EvaluationContext(
                context.Mode,
                lambdaBodyExpression,
                context.JsonContext,
                context.Token,
                context.Scope,
                context.Transform);

            var result = context.JsonContext.ExpressionEvaluator.Evaluate(evaluationContext);

            return result.TransformedToken;
        }

        protected static IJsonToken? LambdaOrDefault<T, TResult>(IEnumerable<T> sequence, LambdaMethod? lambda, Func<IEnumerable<T>, LambdaMethod, Func<Expression, IJsonToken?>, EvaluationContext, IJsonToken?> applyToItems, EvaluationContext context, Func<TResult> useDefault)
        {
            if (lambda is null)
            {
                if (useDefault is null)
                {
                    return default;
                }

                var result = useDefault();

                return context.CreateTokenFrom(result);
            }

            return applyToItems(sequence, lambda, body => ExecuteLambdaBody(lambda.Body, context), context);
        }

        protected static IJsonToken? LambdaOrDefault<T>(IEnumerable<T> sequence, LambdaMethod lambda, Func<IEnumerable<T>, LambdaMethod, Func<Expression, IJsonToken?>, EvaluationContext, IEnumerable<IJsonToken>> applyToItems, EvaluationContext context)
        {
            var results = applyToItems(sequence, lambda, body => ExecuteLambdaBody(lambda.Body, context), context).ToArray();

            return context.CreateArrayFrom(results);
        }
    }
}
