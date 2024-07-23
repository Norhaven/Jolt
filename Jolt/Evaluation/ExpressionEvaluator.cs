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
                BinaryExpression binary => EvaluateBinaryExpression(binary, context),
                _ => default
            };
        }

        private object? EvaluateBinaryExpression(BinaryExpression binary, EvaluationContext context)
        {
            if (binary.IsComparison)
            {
                var left = EvaluateExpression(binary.Left, context) as IComparable;
                var right = EvaluateExpression(binary.Right, context) as IComparable;

                return binary.Operator switch
                {
                    Operator.Equal => (left is null && right is null) || left?.CompareTo(right) == 0,
                    Operator.NotEqual => !(left is null && right is null) && left?.CompareTo(right) != 0,
                    Operator.GreaterThan => left?.CompareTo(right) > 0,
                    Operator.LessThan => left?.CompareTo(right) < 0,
                    Operator.GreaterThanOrEquals => left?.CompareTo(right) >= 0,
                    Operator.LessThanOrEquals => left?.CompareTo(right) <= 0,
                    _ => default
                };
            }
            else
            {
                var left = EvaluateExpression(binary.Left, context);
                var right = EvaluateExpression(binary.Right, context);

                if (left is null || right is null)
                {
                    throw new JoltExecutionException($"Unable to perform math using operator '{binary.Operator}' on a null value");
                }

                if (left is bool || right is bool)
                {
                    throw new JoltExecutionException($"Unable to perform math using operator '{binary.Operator}' on a boolean value");
                }

                if (left is string leftString && right is string rightString)
                {
                    if (binary.Operator == Operator.Addition)
                    {
                        return leftString + rightString;
                    }

                    throw new JoltExecutionException($"Unable to perform math on strings, found unexpected operator '{binary.Operator}' in an expression containing only strings");
                }

                if (left is int leftInt && right is int rightInt)
                {
                    return binary.Operator switch
                    {
                        Operator.Addition => leftInt + rightInt,
                        Operator.Subtraction => leftInt - rightInt,
                        Operator.Multiplication => leftInt * rightInt,
                        Operator.Division => leftInt / rightInt,
                        _ => default
                    };
                }

                if (left is double leftDouble && right is double rightDouble)
                {
                    return binary.Operator switch
                    {
                        Operator.Addition => leftDouble + rightDouble,
                        Operator.Subtraction => leftDouble - rightDouble,
                        Operator.Multiplication => leftDouble * rightDouble,
                        Operator.Division => leftDouble / rightDouble,
                        _ => default
                    };
                }

                if ((left is int || left is double) && (right is int || right is double))
                {
                    var convertedLeftDouble = (double)left;
                    var convertedRightDouble = (double)right;

                    return binary.Operator switch
                    {
                        Operator.Addition => convertedLeftDouble + convertedRightDouble,
                        Operator.Subtraction => convertedLeftDouble - convertedRightDouble,
                        Operator.Multiplication => convertedLeftDouble * convertedRightDouble,
                        Operator.Division => convertedLeftDouble / convertedRightDouble,
                        _ => default
                    };
                }

                throw new JoltExecutionException($"Unable to perform math with mismatched types in expression '{left?.GetType()} {binary.Operator} {right?.GetType()}");
            }
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
