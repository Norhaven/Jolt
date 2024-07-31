using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using Jolt.Parsing;
using Jolt.Structure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Jolt.Evaluation
{
    public sealed class ExpressionEvaluator : IExpressionEvaluator
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

            return new EvaluationResult(context.Token.PropertyName, default, context.JsonContext.JsonTokenReader.CreateTokenFrom(result));
        }

        private object? EvaluateExpression(Expression expression, EvaluationContext context) 
        {
            return expression switch
            {
                RangeExpression range => UnwrapRange(range, context),
                LiteralExpression literal => UnwrapLiteralValue(literal, context),
                PathExpression path => ExtractPath(path, context),
                MethodCallExpression call => ExecuteMethodCall(call, context),
                BinaryExpression binary => EvaluateBinaryExpression(binary, context),
                _ => default
            };
        }

        private object? EvaluateBinaryExpression(BinaryExpression binary, EvaluationContext context)
        {
            var leftResult = EvaluateExpression(binary.Left, context).UnwrapWith(context.JsonContext.JsonTokenReader);
            var rightResult = EvaluateExpression(binary.Right, context).UnwrapWith(context.JsonContext.JsonTokenReader);

            if (binary.IsComparison)
            {
                if (leftResult is null || rightResult is null)
                {
                    return binary.Operator switch
                    { 
                        Operator.Equal => leftResult is null && rightResult is null,
                        Operator.NotEqual => !(leftResult is null && rightResult is null),
                        _ => throw new JoltExecutionException($"Unable to evaluate operator '{binary.Operator}' with arguments '{leftResult}' and '{rightResult}'")
                    };
                }

                if (Numeric.IsSupported(leftResult))
                {
                    var left = new Numeric(leftResult);
                    var right = new Numeric(rightResult);

                    return binary.Operator switch
                    {
                        Operator.Equal => left.Equals(right),
                        Operator.NotEqual => !left.Equals(right),
                        Operator.GreaterThan => left.IsGreaterThan(right),
                        Operator.LessThan => left.IsLessThan(right),
                        Operator.GreaterThanOrEquals => left.IsGreaterThan(right) || left.Equals(right),
                        Operator.LessThanOrEquals => left.IsLessThan(right) || left.Equals(right),
                        _ => throw new JoltExecutionException($"Unable to evaluate unsupported operator '{binary.Operator}' with arguments '{left}' and '{right}'")
                    };
                }
                else
                {
                    return binary.Operator switch
                    { 
                        Operator.Equal => leftResult.Equals(rightResult),
                        Operator.NotEqual => !leftResult.Equals(rightResult),
                        _ => throw new JoltExecutionException($"Unable to evaluate operator '{binary.Operator}' with arguments '{leftResult}' and '{rightResult}'")
                    };
                }
            }
            else
            {
                if (leftResult is null || rightResult is null)
                {
                    throw new JoltExecutionException($"Unable to perform math using operator '{binary.Operator}' on a null value");
                }

                if (leftResult is bool || rightResult is bool)
                {
                    throw new JoltExecutionException($"Unable to perform math using operator '{binary.Operator}' on a boolean value");
                }

                if (leftResult is string leftString && rightResult is string rightString)
                {
                    if (binary.Operator == Operator.Addition)
                    {
                        return leftString + rightString;
                    }

                    throw new JoltExecutionException($"Unable to perform math on strings, found unexpected operator '{binary.Operator}' in an expression containing only strings");
                }

                if (Numeric.IsSupported(leftResult))
                {
                    var left = new Numeric(leftResult);
                    var right = new Numeric(rightResult);

                    return binary.Operator switch
                    {
                        Operator.Addition => left.Add(right),
                        Operator.Subtraction => left.Subtract(right),
                        Operator.Multiplication => left.Multiply(right),
                        Operator.Division => left.Divide(right),
                        _ => default
                    };
                }

                if (binary.Operator == Operator.Addition && leftResult is ICombinable combinable)
                {
                    return combinable.CombineWith(rightResult);
                }

                throw new JoltExecutionException($"Unable to perform math with mismatched types in expression '{leftResult?.GetType()} {binary.Operator} {rightResult?.GetType()}");
            }
        }

        private object ExtractPath(PathExpression path, EvaluationContext context)
        {
            return path.PathQuery;
        }

        private Range UnwrapRange(RangeExpression range, EvaluationContext context)
        {
            return new Range(range.StartIndex,range.EndIndex);
        }

        private object UnwrapLiteralValue(LiteralExpression literal, EvaluationContext context)
        {
            if (literal.Type == typeof(string))
            {
                return literal.Value;
            }

            if (literal.Type == typeof(long))
            {
                if (!long.TryParse(literal.Value, out var numericValue))
                {
                    throw new JoltExecutionException($"Unable to convert value '{literal.Value}' to an integer");
                }

                return numericValue;
            }

            if (literal.Type == typeof(double))
            {
                if (!double.TryParse(literal.Value, out var numericValue))
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
            if (context.Mode == EvaluationMode.PropertyName && !call.Signature.IsAllowedAsPropertyName)
            {
                throw new JoltExecutionException($"Unable to use method '{call.Signature.Alias}' within a property name");
            }

            if (context.Mode == EvaluationMode.PropertyValue && !call.Signature.IsAllowedAsPropertyValue)
            {
                throw new JoltExecutionException($"Unable to use method '{call.Signature.Alias}' within a property value");
            }

            var actualParameterValues = new List<object>();
            var variadicParameterValue = new List<object>();

            // We could have the last parameter prior to the EvaluationContext be variadic
            // so if that's the case then we need to start collecting those differently
            // when we reach that location. For external methods, though, it will be the
            // last parameter because the EvaluationContext is not allowed, so it needs to
            // understand that case as well.

            var numberOfFormalParameters = call.Signature.Parameters.Length;
            var hasEvaluationContext = call.Signature.Parameters[^1].Type == typeof(EvaluationContext);
            var lastParameterIndexFromEnd = hasEvaluationContext && numberOfFormalParameters > 1 ? numberOfFormalParameters - 2 : numberOfFormalParameters - 1;
            var lastParameterIsVariadic = call.Signature.Parameters[lastParameterIndexFromEnd].IsVariadic;

            for(var i = 0; i < call.ParameterValues.Length; i++)
            {
                var parameter = call.ParameterValues[i];
                var currentFormalParameter = (lastParameterIsVariadic && i >= lastParameterIndexFromEnd) ? call.Signature.Parameters[lastParameterIndexFromEnd] : call.Signature.Parameters[i];

                var value = currentFormalParameter.IsLazyEvaluated ? parameter : EvaluateExpression(parameter, context);

                value = value.UnwrapWith(context.JsonContext.JsonTokenReader);

                if (currentFormalParameter.IsVariadic)
                {
                    variadicParameterValue.Add(value);
                }
                else
                {
                    actualParameterValues.Add(value);
                }
            }

            if (lastParameterIsVariadic)
            {
                actualParameterValues.Add(variadicParameterValue.ToArray());
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
                // We may have gotten a sequence of either array elements or object properties back
                // with that call so we need to iterate over them and populate the appropriate target structure.

                if (context.Token.CurrentTransformerToken.Type == JsonTokenType.Array)
                {
                    resultValue = context.JsonContext.JsonTokenReader.CreateArrayFrom((IEnumerable<IJsonToken>)resultValue);
                }
                else if (context.Token.CurrentTransformerToken.Type == JsonTokenType.Object)
                {
                    resultValue = context.JsonContext.JsonTokenReader.CreateObjectFrom((IEnumerable<IJsonToken>)resultValue);
                }
            }

            if (context.Mode == EvaluationMode.PropertyName)
            {
                // The method may have been a value generator, meaning that evaluating the property name will
                // also cause the value for that property to be generated (e.g. the loop method) but if we're
                // taking a look at a non-generator then this needs to be flagged when it's sent back so that
                // the transformer follows up with evaluating the value as well and doesn't just assume that
                // it's already happened.

                if (!call.Signature.IsValueGenerator && resultValue is IJsonProperty property)
                {
                    return new EvaluationResult(context.Token.PropertyName, property.PropertyName, context.Token.CurrentTransformerToken, true);
                }

                var resolvedPropertyName = call.Signature.IsValueGenerator ? context.Token.ResolvedPropertyName : ((IJsonProperty)context.Token.CurrentSource.Property)?.PropertyName;

                return new EvaluationResult(context.Token.PropertyName, resolvedPropertyName, (IJsonToken)resultValue); 
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
                var type = Type.GetType(method.AssemblyQualifiedTypeName);
                var methodInfo = type.GetMethod(method.Name, BindingFlags.Public | BindingFlags.Static);

                return methodInfo.Invoke(null, actualParameterValues.ToArray());
            }
            else if (method.CallType == CallType.Instance)
            {
                if (context.JsonContext.MethodContext is null)
                {
                    throw new JoltExecutionException($"Unable to execute external instance method '{method.Name}', no method context was provided");
                }

                var methodInfo = context.JsonContext.MethodContext.GetType().GetMethod(method.Name, BindingFlags.Public | BindingFlags.Instance);

                if (methodInfo is null)
                {
                    throw new JoltExecutionException($"Unable to locate instance method '{method.Name}' on type '{context.JsonContext.MethodContext.GetType().FullName}'");
                }

                return methodInfo.Invoke(context.JsonContext.MethodContext, actualParameterValues.ToArray());
            }

            throw new JoltExecutionException($"Unable to invoke method with unknown call type '{method.CallType}'");
        }
    }
}
