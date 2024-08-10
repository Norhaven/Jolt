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
                return new EvaluationResult(context.Token.PropertyName, default, token, rangeVariable: context.Token.ParentRangeVariable);
            }

            if (result is RangeVariable range)
            {
                var isValuePendingEvaluation = context.Mode == EvaluationMode.PropertyName && range.Value is null;

                return new EvaluationResult(context.Token.PropertyName, default, range.Value, isValuePendingEvaluation, range);
            }

            return new EvaluationResult(context.Token.PropertyName, default, context.JsonContext.JsonTokenReader.CreateTokenFrom(result));
        }

        private object? EvaluateExpression(Expression expression, EvaluationContext context) 
        {
            return expression switch
            {
                RangeExpression range => UnwrapRange(range, context),
                RangeVariableExpression range => UnwrapRangeVariable(range, context),
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

            if (leftResult is RangeVariable leftResultVariable)
            {
                leftResult = leftResultVariable.Value.ToTypeOf<object>();
            }

            if (rightResult is RangeVariable rightResultVariable)
            {
                rightResult = rightResultVariable.Value.ToTypeOf<object>();
            }

            if (binary.IsComparison)
            {
                if (leftResult is null || rightResult is null)
                {
                    return binary.Operator switch
                    { 
                        Operator.Equal => leftResult is null && rightResult is null,
                        Operator.NotEqual => !(leftResult is null && rightResult is null),
                        _ => throw Error.CreateExecutionErrorFrom(ExceptionCode.UnableToEvaluateExpressionWithOperatorAndArguments, leftResult, binary.Operator, rightResult)
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
                        _ => throw Error.CreateExecutionErrorFrom(ExceptionCode.UnableToEvaluateExpressionWithOperatorAndArguments, left, binary.Operator, right)
                    };
                }
                else
                {
                    return binary.Operator switch
                    { 
                        Operator.Equal => leftResult.Equals(rightResult),
                        Operator.NotEqual => !leftResult.Equals(rightResult),
                        _ => throw Error.CreateExecutionErrorFrom(ExceptionCode.UnableToEvaluateExpressionWithOperatorAndArguments, leftResult, binary.Operator, rightResult)
                    };
                }
            }
            else
            {
                if (leftResult is null || rightResult is null)
                {
                    throw Error.CreateExecutionErrorFrom(ExceptionCode.UnableToEvaluateExpressionWithNullArgument, binary.Operator);
                }

                if (leftResult is bool || rightResult is bool)
                {
                    throw Error.CreateExecutionErrorFrom(ExceptionCode.UnableToEvaluateExpressionWithBooleanArgument, binary.Operator);
                }

                if (leftResult is string leftString && rightResult is string rightString)
                {
                    if (binary.Operator == Operator.Addition)
                    {
                        return leftString + rightString;
                    }

                    throw Error.CreateExecutionErrorFrom(ExceptionCode.UnableToEvaluateExpressionWithOnlyStrings, binary.Operator);
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

                throw Error.CreateExecutionErrorFrom(ExceptionCode.UnableToEvaluateExpressionWithMismatchedTypes, leftResult?.GetType(), binary.Operator, rightResult?.GetType());
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

        private RangeVariable UnwrapRangeVariable(RangeVariableExpression range, EvaluationContext context)
        {
            if (!context.RangeVariables.TryPeek(out var variables))
            {
                return new RangeVariable(range.Name);
            }

            var existingVariable = variables.FirstOrDefault(x => x.Name == range.Name);

            if (existingVariable is null)
            {
                return new RangeVariable(range.Name);
            }

            return existingVariable;
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
                    throw Error.CreateExecutionErrorFrom(ExceptionCode.UnableToConvertValueToInteger, literal.Value);
                }

                return numericValue;
            }

            if (literal.Type == typeof(double))
            {
                if (!double.TryParse(literal.Value, out var numericValue))
                {
                    throw Error.CreateExecutionErrorFrom(ExceptionCode.UnableToConvertValueToDecimal, literal.Value);
                }

                return numericValue;
            }

            if (literal.Type == typeof(bool))
            {
                if (!bool.TryParse(literal.Value, out var booleanValue))
                {
                    throw Error.CreateExecutionErrorFrom(ExceptionCode.UnableToConvertValueToBoolean, literal.Value);
                }

                return booleanValue;
            }

            throw Error.CreateExecutionErrorFrom(ExceptionCode.UnableToConvertToBestTypeForLiteralValue, literal.Value);
        }

        private object? ExecuteMethodCall(MethodCallExpression call, EvaluationContext context)
        {
            if (context.Mode == EvaluationMode.PropertyName && !call.Signature.IsAllowedAsPropertyName)
            {
                throw Error.CreateExecutionErrorFrom(ExceptionCode.UnableToUseMethodWithinPropertyName, call.Signature.Alias);
            }

            if (context.Mode == EvaluationMode.PropertyValue && !call.Signature.IsAllowedAsPropertyValue)
            {
                throw Error.CreateExecutionErrorFrom(ExceptionCode.UnableToUseMethodWithinPropertyValue, call.Signature.Alias);
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
            var lastParameterIsOptional = call.Signature.Parameters[lastParameterIndexFromEnd].IsOptional;

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
            else if (lastParameterIsOptional && call.ParameterValues.Length < lastParameterIndexFromEnd + 1)
            {
                var optionalDefaultValue = call.Signature.Parameters[lastParameterIndexFromEnd].OptionalDefaultValue;

                actualParameterValues.Add(optionalDefaultValue);
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

            if (call.Signature.Parameters.Length > actualParameterValues.Count)
            {
                var nextMissingParameter = call.Signature.Parameters[actualParameterValues.Count - 1];

                throw Error.CreateExecutionErrorFrom(ExceptionCode.MissingRequiredMethodParameter, call.Signature.Name, nextMissingParameter.Name, nextMissingParameter.Type);
            }
            else if (actualParameterValues.Count > call.Signature.Parameters.Length && !lastParameterIsVariadic)
            {
                throw Error.CreateExecutionErrorFrom(ExceptionCode.MethodCallActualParameterCountExceedsFormalParameterCount, call.Signature.Name, call.Signature.Parameters.Length);
            }

            var resultValue = InvokeMethod(call.Signature, actualParameterValues, context);

            // An enumerable sequence of JSON tokens is possible to get back from a method call, such as the
            // result of looping over an object or array, and those tokens are orphans that need to be
            // collected and put into the appropriate structure before sending back to the caller.
            // We could also potentially get back a JSON object or array itself, which is already
            // contained and does not need to be packed up into a structure even though they can be enumerated
            // as a sequence of tokens, so we're disallowing the repackaging operation for those types here.

            var isResultObjectOrArray = resultValue is IJsonObject || resultValue is IJsonArray;

            if (!isResultObjectOrArray && typeof(IEnumerable<IJsonToken>).IsAssignableFrom(resultValue?.GetType()))
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
                // taking a look at a non-generator property then this needs to be flagged when it's sent back so that
                // the transformer follows up with evaluating the value as well and doesn't just assume that
                // it's already happened.

                if (!call.Signature.IsValueGenerator && resultValue is IJsonProperty property)
                {
                    return new EvaluationResult(context.Token.PropertyName, property.PropertyName, context.Token.CurrentTransformerToken, true);
                }

                var resolvedPropertyName = call.Signature.IsValueGenerator ? context.Token.ResolvedPropertyName : ((IJsonProperty)context.Token.CurrentSource.Property)?.PropertyName;

                return new EvaluationResult(context.Token.PropertyName, resolvedPropertyName, (IJsonToken)resultValue, rangeVariable: context.Token.ParentRangeVariable ?? call.GeneratedVariable); 
            }
            else if (context.Mode == EvaluationMode.PropertyValue)
            {
                return resultValue;
            }

            throw Error.CreateExecutionErrorFrom(ExceptionCode.UnableToApplyMethodResultsWithUnknownEvaluationMode, context.Mode);
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
                    throw Error.CreateExecutionErrorFrom(ExceptionCode.UnableToInvokeInstanceMethodWithoutMethodContext, method.Name);
                }

                var methodInfo = context.JsonContext.MethodContext.GetType().GetMethod(method.Name, BindingFlags.Public | BindingFlags.Instance);

                if (methodInfo is null)
                {
                    throw Error.CreateExecutionErrorFrom(ExceptionCode.UnableToLocateInstanceMethodWithProvidedMethodContext, method.Name, context.JsonContext.MethodContext.GetType().FullName);
                }

                return methodInfo.Invoke(context.JsonContext.MethodContext, actualParameterValues.ToArray());
            }

            throw Error.CreateExecutionErrorFrom(ExceptionCode.UnableToInvokeMethodWithUnknownCallType, method.CallType);
        }
    }
}
