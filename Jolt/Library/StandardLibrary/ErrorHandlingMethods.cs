using Jolt.Evaluation;
using Jolt.Expressions;
using Jolt.Extensions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Library.StandardLibrary
{
    [IncludeInStandardLibrary]
    internal sealed class ErrorHandlingMethods : UnderlyingExecutionMethods
    {
        [JoltLibraryMethod("try")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName | LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? Try([LazyEvaluation] Expression body, LambdaMethod handleError, EvaluationContext context)
        {
            try
            {
                var evaluationContext = new EvaluationContext(
                    context.Mode,
                    body,
                    context.JsonContext,
                    context.Token,
                    context.Scope,
                    context.Transform);

                var result = context.JsonContext.ExpressionEvaluator.Evaluate(evaluationContext);

                return result.TransformedToken;
            }
            catch (Exception ex)
            {
                var error = new EvaluationError(ex);

                var itemToken = context.CreateTokenFrom(error);
                var errorVariable = new RangeVariable(handleError.Variable.Name, itemToken);

                context.Scope.AddOrUpdateVariable(errorVariable);

                try
                {
                    return ExecuteLambdaBody(handleError.Body, context);
                }
                finally
                {
                    context.Scope.RemoveCurrentVariablesLayer();
                }
            }
        }
    }
}
