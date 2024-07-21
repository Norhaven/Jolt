using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Parsing;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Evaluation
{
    public class ExpressionEvaluator : IExpressionEvaluator
    {
        public EvaluationResult Evaluate(EvaluationContext context)
        {
            var result = EvaluateExpression(context.Expression, context);

            if (result is EvaluationResult evaluationResult)
            {
                return evaluationResult;
            }

            if (result is IJsonToken token)
            {
                return new EvaluationResult(context.Token.PropertyName, default, token);
            }

            throw new JoltExecutionException($"Expression evaluation did not generate a JSON token, unable to continue with type '{result?.GetType()}'");
        }

        private object? EvaluateExpression(Expression expression, EvaluationContext context) 
        {
            return expression switch
            {
                LiteralExpression literal => UnwrapLiteralValue(literal, context),
                PathExpression path => ExtractPath(path, context),
                MethodCallExpression call => ExecuteMethodCall(call, context),
                _ => default
            };
        }

        private object ExtractPath(PathExpression path, EvaluationContext context)
        {
            return path.PathQuery;
        }

        private object UnwrapLiteralValue(LiteralExpression literal, EvaluationContext context)
        {
            if (literal.Type == typeof(string))
            {
                return literal.Value;
            }

            if (literal.Type == typeof(int))
            {
                if (!int.TryParse(literal.Value, out var numericValue))
                {
                    throw new JoltExecutionException($"Unable to convert value '{literal.Value}' to an integer");
                }

                return numericValue;
            }

            if (literal.Type == typeof(bool))
            {
                if (!bool.TryParse(literal.Value, out var booleanValue))
                {
                    throw new JoltExecutionException($"Unable to convert value '{literal.Value}' to an boolean");
                }

                return booleanValue;
            }

            throw new JoltExecutionException($"Unable to convert to best type for literal value '{literal.Value}'");
        }

        private object? ExecuteMethodCall(MethodCallExpression call, EvaluationContext context)
        {
            var actualParameterValues = new List<object>();

            foreach(var parameter in call.ParameterValues)
            {
                var value = EvaluateExpression(parameter, context);

                actualParameterValues.Add(value);
            }

            if (call.Signature.IsSystemMethod)
            {
                actualParameterValues.Add(context);
            }

            if (context.Mode == EvaluationMode.PropertyName)
            {
                context.Token.ResolvedPropertyName = context.Expression switch
                {
                    MethodCallExpression method => method.GeneratedName,
                    _ => default
                };
            }

            var resultValue = InvokeMethod(call.Signature, actualParameterValues, context);

            if (typeof(IEnumerable<IJsonToken>).IsAssignableFrom(resultValue?.GetType()))
            {
                resultValue = context.Context.JsonTokenReader.CreateArrayFrom((IEnumerable<IJsonToken>)resultValue);
            }

            if (context.Mode == EvaluationMode.PropertyName)
            {
                return new EvaluationResult(context.Token.PropertyName, context.Token.ResolvedPropertyName, (IJsonToken)resultValue); 
            }
            else if (context.Mode == EvaluationMode.PropertyValue)
            {
                return resultValue;
            }

            throw new JoltExecutionException($"Unable to apply results of method invocation for unknown evaluation mode '{context.Mode}'");
        }

        private static object? InvokeMethod(MethodSignature method, IEnumerable<object?> actualParameterValues, EvaluationContext context)
        {
            if (method.CallType == CallType.Static)
            {
                var type = Type.GetType($"{method.TypeName}, {method.Assembly}");
                var methodInfo = type.GetMethod(method.Name);

                return methodInfo.Invoke(null, actualParameterValues.ToArray());
            }
            else if (method.CallType == CallType.Instance)
            {
                var methodInfo = context.Context.GetType().GetMethod(method.Name);

                return methodInfo.Invoke(context.Context, actualParameterValues.ToArray());
            }

            throw new JoltExecutionException($"Unable to invoke method with unknown call type '{method.CallType}'");
        }
    }
}
