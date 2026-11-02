using Jolt.Evaluation.Matching;
using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Extensions;
using Jolt.Library;
using Jolt.Library.StandardLibrary;
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
        private readonly JoltOptions _options;

        public ExpressionEvaluator(JoltOptions options)
        {
            _options = options;
        }

        public EvaluationResult Evaluate(EvaluationContext context)
        {
            var result = EvaluateExpression(context.Expression, context, isRootExpression: true);

            // A path on its own (e.g. a lambda body or the branch of an #if) gives the value at that path.

            result = context.ResolveQueryPathIfPresent(result);

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
                // When the variable already has a value that means that we're just directly using whatever variable
                // we have as the result of the property value evaluation and that should take precedence over any
                // existing transformer token. Otherwise, fall back to using the literal value to allow for setting
                // variables with literal values as well as expression results.

                var isValuePendingEvaluation = context.Mode == EvaluationMode.PropertyName && range.Value is null;

                var actualTransformerToken = isValuePendingEvaluation ? context.Token.CurrentTransformerToken : range.Value;

                return new EvaluationResult(context.Token.PropertyName, default, actualTransformerToken, isValuePendingEvaluation, range);
            }

            if (result is DereferencedPath path)
            {
                return new EvaluationResult(context.Token.PropertyName, default, path.ObtainableToken);
            }

            return new EvaluationResult(context.Token.PropertyName, default, context.JsonContext.JsonTokenReader.CreateTokenFrom(result));
        }

        private object? EvaluateExpression(Expression expression, EvaluationContext context, bool isRootExpression = false)
        {
            return expression switch
            {
                RangeExpression range => EvaluateRange(range, context),
                RangeVariableExpression range => UnwrapRangeVariable(range, context),
                PropertyDereferenceExpression dereference => UnwrapDereferenceChain(dereference, context),
                SlicedVariableExpression slicedVariable => UnwrapSlicedVariable(slicedVariable, context, isRootExpression),
                EnumerateAsVariableExpression enumerate => UnwrapEnumeration(enumerate, context),
                VariableAliasExpression variable => UnwrapVariableAlias(variable, context),
                LiteralExpression literal => UnwrapLiteralValue(literal, context),
                PathExpression path => ExtractPath(path, context),
                MethodCallExpression call => EndChain(ExecuteMethodCall(call, context, isRootExpression)),
                BinaryExpression binary => EvaluateBinaryExpression(binary, context),
                LambdaMethodExpression lambda => EvaluateLambdaExpression(lambda, context),
                ArrayLiteralExpression array => EvaluateArrayLiteral(array, context),
                ObjectLiteralExpression obj => EvaluateObjectLiteral(obj, context),
                LogicalNotExpression not => EvaluateLogicalNotExpression(not, context),
                TypeLiteralExpression type => UnwrapTypeLiteral(type, context),
                DiscardExpression discard => UnwrapDiscard(discard, context),
                _ => default
            };
        }

        private object? EvaluateLogicalNotExpression(LogicalNotExpression not, EvaluationContext context)
        {
            var expressionResult = ResolveOperandValue(EvaluateExpression(not.Operand, context).UnwrapWith(context.JsonContext.JsonTokenReader), context);

            if (!(expressionResult is bool value))
            {
                throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToEvaluateLogicalNotExpressionWithNonBooleanOperand, expressionResult?.GetType());
            }

            return !value;
        }

        private object? EvaluateArrayLiteral(ArrayLiteralExpression array, EvaluationContext context)
        {
            if (array.Elements.Length == 0)
            {
                return context.CreateArrayFrom(Array.Empty<IJsonToken>());
            }

            var jsonElements = from element in array.Elements
                               select EvaluateExpression(element, context) into evaluatedElement
                               let isPatternMatchVariable = evaluatedElement is RangeVariable variable && !context.Scope.ContainsVariable(variable.Name)
                               let value = isPatternMatchVariable && evaluatedElement is RangeVariable variable ? new MatchDiscard(MatchKind.Variable, variable.Name) : evaluatedElement
                               select context.CreateTokenFrom(context.ResolveQueryPathIfPresent(value));

            return context.CreateArrayFrom(jsonElements.ToArray());
        }

        private object? EvaluateObjectLiteral(ObjectLiteralExpression obj, EvaluationContext context)
        {
            var jsonProperties = from property in obj.Properties
                                 let propertyName = property.PropertyName
                                 let propertyValue = EvaluateExpression(property.PropertyValue, context)
                                 let isPatternMatchVariable = propertyValue is RangeVariable variable && !context.Scope.ContainsVariable(variable.Name)
                                 let value = isPatternMatchVariable && propertyValue is RangeVariable variable ? new MatchDiscard(MatchKind.Variable, variable.Name) : propertyValue
                                 select new KeyValuePair<string, IJsonToken>(propertyName, context.CreateTokenFrom(context.ResolveQueryPathIfPresent(value)));

            return context.CreateTokenFrom(jsonProperties.ToDictionary(x => x.Key, x => x.Value));
        }

        private object? UnwrapEnumeration(EnumerateAsVariableExpression enumerate, EvaluationContext context)
        {
            var variable = UnwrapRangeVariable(enumerate.Variable, context);
            var source = EvaluateExpression(enumerate.EnumerationSource, context);

            var indexVariable = enumerate.Variable switch
            {
                RangeVariablePairExpression pair => UnwrapRangeVariable(pair.SecondVariable, context),
                _ => default
            };

            return new Enumeration(variable, source, indexVariable);
        }

        private object? UnwrapVariableAlias(VariableAliasExpression expression, EvaluationContext context)
        {
            var source = expression.IsSourceFromPath ? ExtractPath(expression.SourcePath, context) : UnwrapRangeVariable(expression.SourceVariable, context);
            var aliasVariable = UnwrapRangeVariable(expression.AliasVariable, context);

            return new VariableAlias(source, aliasVariable);
        }

        private object? UnwrapDiscard(DiscardExpression discard, EvaluationContext context)
        {
            return new MatchDiscard(MatchKind.Discard);
        }

        private object? UnwrapTypeLiteral(TypeLiteralExpression expression, EvaluationContext context)
        {
            return expression.TypeName switch
            {
                "array" => MatchCaseType.Array,
                "object" => MatchCaseType.Object,
                "string" => MatchCaseType.String,
                "integer" => MatchCaseType.Integer,
                "decimal" => MatchCaseType.Decimal,
                "boolean" => MatchCaseType.Boolean,
                "null" => MatchCaseType.Null,
                _ => default
            };
        }

        private object? EvaluateBinaryExpression(BinaryExpression binary, EvaluationContext context)
        {
            object? UnpackAsDereferencedPathIfPresent(object? potentialPath)
            {
                if (potentialPath is DereferencedPath path)
                {
                    // A missing property, or a chain skipped by a null-safe dereference, gives null.

                    return path.MissingPaths.Length > 0 ? null : path.ObtainableToken?.ToTypeOf<object>();
                }

                return potentialPath;
            }

            object? EvaluateOperand(Expression operand)
            {
                var result = EvaluateExpression(operand, context).UnwrapWith(context.JsonContext.JsonTokenReader);

                result = UnpackAsDereferencedPathIfPresent(result);

                return ResolveOperandValue(result, context);
            }

            var leftResult = EvaluateOperand(binary.Left);

            // The logical and null-coalescing operators only evaluate their right operand when the left one doesn't
            // already decide the result, so that the left can guard against something the right would fail on.

            if (binary.Operator == Operator.LogicalAnd || binary.Operator == Operator.LogicalOr)
            {
                if (!(leftResult is bool leftBool))
                {
                    throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToEvaluateLogicalExpressionWithNonBooleanArgument, leftResult?.GetType(), binary.Operator);
                }

                if (binary.Operator == Operator.LogicalAnd && !leftBool)
                {
                    return false;
                }

                if (binary.Operator == Operator.LogicalOr && leftBool)
                {
                    return true;
                }

                var rightLogicalResult = EvaluateOperand(binary.Right);

                if (!(rightLogicalResult is bool rightBool))
                {
                    throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToEvaluateLogicalExpressionWithNonBooleanArgument, rightLogicalResult?.GetType(), binary.Operator);
                }

                return rightBool;
            }

            if (binary.Operator == Operator.NullCoalescing)
            {
                return leftResult ?? EvaluateOperand(binary.Right);
            }

            var rightResult = EvaluateOperand(binary.Right);

            if (binary.IsComparison)
            {
                if (leftResult is null || rightResult is null)
                {
                    return binary.Operator switch
                    { 
                        Operator.Equals => leftResult is null && rightResult is null,
                        Operator.NotEquals => !(leftResult is null && rightResult is null),
                        _ => throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToEvaluateExpressionWithOperatorAndArguments, leftResult, binary.Operator, rightResult)
                    };
                }

                if (Numeric.IsSupported(leftResult))
                {
                    var left = new Numeric(leftResult);
                    var right = new Numeric(rightResult);

                    return binary.Operator switch
                    {
                        Operator.Equals => left.Equals(right),
                        Operator.NotEquals => !left.Equals(right),
                        Operator.GreaterThan => left.IsGreaterThan(right),
                        Operator.LessThan => left.IsLessThan(right),
                        Operator.GreaterThanOrEquals => left.IsGreaterThan(right) || left.Equals(right),
                        Operator.LessThanOrEquals => left.IsLessThan(right) || left.Equals(right),
                        _ => throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToEvaluateExpressionWithOperatorAndArguments, left, binary.Operator, right)
                    };
                }
                else
                {
                    return binary.Operator switch
                    { 
                        Operator.Equals => leftResult.Equals(rightResult),
                        Operator.NotEquals => !leftResult.Equals(rightResult),
                        _ => throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToEvaluateExpressionWithOperatorAndArguments, leftResult, binary.Operator, rightResult)
                    };
                }
            }
            else
            {
                if (leftResult is null || rightResult is null)
                {
                    throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToEvaluateExpressionWithNullArgument, binary.Operator);
                }

                if (leftResult is bool || rightResult is bool)
                {
                    throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToEvaluateExpressionWithBooleanArgument, binary.Operator);
                }

                if (leftResult is string leftString && rightResult is string rightString)
                {
                    if (binary.Operator == Operator.Addition)
                    {
                        return leftString + rightString;
                    }

                    throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToEvaluateExpressionWithOnlyStrings, binary.Operator);
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

                throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToEvaluateExpressionWithMismatchedTypes, leftResult?.GetType(), binary.Operator, rightResult?.GetType());
            }
        }

        private LambdaMethod EvaluateLambdaExpression(LambdaMethodExpression lambda, EvaluationContext context)
        {
            var variable = UnwrapRangeVariable(lambda.Variable, context);
            var secondVariable = lambda.Variable is RangeVariablePairExpression pair ? UnwrapRangeVariable(pair.SecondVariable, context) : null;

            return new LambdaMethod(variable, secondVariable, lambda.Body);
        }

        private object ExtractPath(PathExpression path, EvaluationContext context)
        {
            return new QueryPathReference(path.PathQuery);
        }

        /// <summary>
        /// Gets the plain value of an operand, resolving a path or variable to the value it holds.
        /// </summary>
        private static object? ResolveOperandValue(object? value, EvaluationContext context)
        {
            return value switch
            {
                QueryPathReference path => context.SelectTokenAt(path)?.ToTypeOf<object>(),
                RangeVariable variable => variable.Value?.ToTypeOf<object>(),
                _ => value
            };
        }

        private Range EvaluateRange(RangeExpression range, EvaluationContext context)
        {
            var leftResult = EvaluateExpression(range.StartIndex.Index, context).UnwrapWith(context.JsonContext.JsonTokenReader);
            var rightResult = EvaluateExpression(range.EndIndex.Index, context).UnwrapWith(context.JsonContext.JsonTokenReader);

            if (leftResult is null || rightResult is null)
            {
                throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToEvaluateRangeWithNullIndex, leftResult?.GetType(), rightResult?.GetType());
            }

            if (leftResult is RangeVariable leftVariable)
            {
                leftResult = leftVariable.Value?.ToTypeOf<long>();
            }

            if (rightResult is RangeVariable rightVariable)
            {
                rightResult = rightVariable.Value?.ToTypeOf<long>();
            }

            if (leftResult is long left && rightResult is long right)
            {
                var isIdenticalIndices = range.IsSingleIndex;
                var leftIndex = new Index((int)left, range.StartIndex.IsOffsetFromEnd);

                var rightIndex = (isIdenticalIndices, range.EndIndex.IsOffsetFromEnd) switch
                {
                    (true, true) => new Index((int)right - 1, true),
                    (true, false) => new Index((int)right + 1, false),
                    (false, _) => new Index((int)right, range.EndIndex.IsOffsetFromEnd)
                };

                return new Range(leftIndex, rightIndex);
            }

            throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToEvaluateRangeWithNonIntegerIndex, leftResult.GetType(), rightResult.GetType());
        }

        private RangeVariable UnwrapRangeVariable(RangeVariableExpression range, EvaluationContext context)
        {
            if (!context.Scope.TryGetVariable(range.Name, out var variable))
            {
                return new RangeVariable(range.Name, range.ProvidesNullSafeAccess);
            }

            return variable;
        }

        private object? UnwrapSlicedVariable(SlicedVariableExpression slicedVariable, EvaluationContext context, bool isRootExpression)
        {
            var variable = UnwrapRangeVariable(slicedVariable.Variable, context);

            object? value = variable.Value switch
            {
                IJsonValue val when val.ValueType == JsonValueType.String => val.ToTypeOf<string>(),
                IJsonValue val when val.ValueType == JsonValueType.Null => null,
                IJsonArray array => array,
                _ => null
            };

            if (value is null)
            {
                throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.AttemptedToIndexOrSliceNullVariableValue, variable.Name);
            }

            return IndexOrSliceWithRange(slicedVariable.Range, variable.Value, context, isRootExpression);
        }

        private object? IndexOrSliceWithRange(RangeExpression indexOrSliceRange, IJsonToken value, EvaluationContext context, bool isRootExpression)
        {
            var closure = context.Scope.CreateClosureOver(value);

            var variableName = Guid.NewGuid().ToString();

            closure.AddOrUpdateVariable(new RangeVariable(variableName, value));

            try
            {
                if (value is IJsonValue val && val.IsString())
                {
                    var method = context.JsonContext.ReferenceResolver.GetMethod(nameof(StringAndArrayMethods.Substring).ToLowerInvariant());
                    var call = new MethodCallExpression(method, new Expression[] { new RangeVariableExpression(variableName), indexOrSliceRange });

                    return ExecuteMethodCall(call, context, isRootExpression);
                }
                else if (value is IJsonArray array)
                {
                    var method = context.JsonContext.ReferenceResolver.GetMethod(nameof(StringAndArrayMethods.Slice).ToLowerInvariant());
                    var call = new MethodCallExpression(method, new Expression[] { new RangeVariableExpression(variableName), indexOrSliceRange });

                    var slice = ExecuteMethodCall(call, context, isRootExpression);

                    // A single index (e.g. "[1]") gives the element itself, while a range (e.g. "[1..2]") always gives an array.

                    return indexOrSliceRange.IsSingleIndex && slice is IJsonArray elements
                        ? elements.First()
                        : slice;
                }
                else
                {
                    throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.AttemptedToIndexOrSliceNonStringAndNonArrayValue, value.GetType().Name);
                }
            }
            finally
            {
                closure.RemoveCurrentClosure();
            }
        }

        private DereferencedPath UnwrapDereferenceChain(PropertyDereferenceExpression dereference, EvaluationContext context)
        {
            var rangeVariable = EvaluateExpression(dereference.Variable, context) as RangeVariable;

            var currentProperty = rangeVariable?.Value;
            var currentName = rangeVariable?.Name ?? dereference.Variable.Name;

            for (var i = 0; i < dereference.DereferenceChain.Length; i++)
            {
                var propertyReference = dereference.DereferenceChain[i];

                // Following C#, a null-safe dereference guards the value to its left: '@x?.first' gives null without looking for
                // 'first' when '@x' is null or missing, and skips the rest of the chain (including any piped methods) along with it.
                // A null-safe variable (e.g. '@x?.first') is the same as a null-safe dereference of its first property.

                var isNullSafe = propertyReference.IsNullSafe || (i == 0 && dereference.Variable.ProvidesNullSafeAccess == true);

                if (ValueResolutionExtensions.IsNull(currentProperty))
                {
                    if (isNullSafe)
                    {
                        var skippedDereferencePaths = dereference.DereferenceChain[i..].Select(x => x.PropertyName).ToArray();

                        return new DereferencedPath(rangeVariable, null, skippedDereferencePaths, isNullSafe: true);
                    }

                    throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.AttemptedToDereferenceMissingPath, propertyReference.PropertyName, currentName);
                }

                if (currentProperty is IJsonObject json)
                {
                    var node = json[propertyReference.PropertyName];

                    // Both JSON libraries give null for a property containing a JSON null, so it's only missing if the property isn't there.
                    // Statement methods still get the containing object for it, so that '#setAt' can replace the null.

                    if (node is null && !context.Token.IsWithinStatementBlock && json.HasProperty(propertyReference.PropertyName))
                    {
                        node = context.CreateNullToken();
                    }

                    if (node is null)
                    {
                        var missingDereferencePaths = dereference.DereferenceChain[i..].Select(x => x.PropertyName).ToArray();

                        // Statement methods (e.g. '#setAt') need the deepest object that exists, so that they can add the rest of the path to it.

                        if (context.Token.IsWithinStatementBlock)
                        {
                            return new DereferencedPath(rangeVariable, json, missingDereferencePaths);
                        }

                        // A missing last property gives null, while one with more properties to dereference after it is an error (unless null-safe).

                        if (i == dereference.DereferenceChain.Length - 1)
                        {
                            return new DereferencedPath(rangeVariable, null, missingDereferencePaths);
                        }
                    }

                    currentProperty = node;
                    currentName = propertyReference.PropertyName;
                }
                else if (currentProperty is IJsonValue)
                {
                    throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.EncounteredValueInDereferenceChainButExpectedObject, propertyReference.PropertyName);
                }
                else
                {
                    throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.EncounteredNonObjectInDereferenceChainButExpectedObject, propertyReference.PropertyName);
                }
            }

            return new DereferencedPath(rangeVariable, currentProperty);
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
                    throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToConvertValueToInteger, literal.Value);
                }

                return numericValue;
            }

            if (literal.Type == typeof(double))
            {
                if (!double.TryParse(literal.Value, out var numericValue))
                {
                    throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToConvertValueToDecimal, literal.Value);
                }

                return numericValue;
            }

            if (literal.Type == typeof(bool))
            {
                if (!bool.TryParse(literal.Value, out var booleanValue))
                {
                    throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToConvertValueToBoolean, literal.Value);
                }

                return booleanValue;
            }

            if (literal.Type == typeof(object) && literal.Value is null)
            {
                return default;
            }

            throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToConvertToBestTypeForLiteralValue, literal.Value);
        }

        private object? ExecuteMethodCall(MethodCallExpression call, EvaluationContext context, bool isRootExpression)
        {
            if (call.Signature.IsUnsafe && !_options.AllowUnsafeEvaluations)
            {
                throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnsafeMethodCallNotAllowed, call.Signature.Name);
            }

            // Method calls in the arguments of a property name method are evaluated as property values (see below),
            // so these checks require only the property name method itself to be valid in a property name.

            if (context.Mode == EvaluationMode.PropertyName && !call.Signature.IsAllowedAsPropertyName)
            {
                throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToUseMethodWithinPropertyName, call.Signature.Alias);
            }

            if (context.Mode == EvaluationMode.PropertyValue && !call.Signature.IsAllowedAsPropertyValue)
            {
                throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToUseMethodWithinPropertyValue, call.Signature.Alias);
            }

            if (!context.Token.IsWithinMatchBlock && call.Signature.IsAllowedAsMatchCase)
            {
                throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToUseMatchCaseMethodOutsideOfMatchBlock, call.Signature.Alias);
            }

            if (isRootExpression && context.Mode == EvaluationMode.PropertyValue && context.Token.IsWithinStatementBlock && !call.Signature.IsAllowedAsStatement)
            {
                throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToUseMethodWithinStatementBlock, call.Signature.Alias);
            }

            if (isRootExpression && context.Mode == EvaluationMode.PropertyName && context.Token.IsWithinMatchBlock && !call.Signature.IsAllowedAsMatchCase)
            {
                throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToUseMethodWithinMatchBlock, call.Signature.Alias);
            }

            if (!isRootExpression && context.Token.IsWithinStatementBlock && call.Signature.IsAllowedAsStatement)
            {
                throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToUseMethodWithinNonRootStatementBlock, call.Signature.Alias);
            }

            if (!context.Token.IsWithinStatementBlock && call.Signature.IsAllowedAsStatement)
            {
                throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToUseMethodOutsideOfStatementBlock, call.Signature.Alias);
            }

            // The arguments of a method call in a property name are values, so they're evaluated as property values.
            // Otherwise, a method call within them would be treated as though it determined the property itself. That
            // also applies to anything evaluated later on their behalf, such as lambda bodies or lazy arguments.

            var argumentContext = context.Mode == EvaluationMode.PropertyName
                ? new EvaluationContext(EvaluationMode.PropertyValue, context.Expression, context.JsonContext, context.Token, context.Scope, context.Transform)
                : context;

            var actualParameterValues = new List<object>();
            var variadicParameterValue = new List<object>();

            // We could have the last parameter prior to the EvaluationContext be variadic
            // so if that's the case then we need to start collecting those differently
            // when we reach that location. For external methods, though, it will be the
            // last parameter because the EvaluationContext is not allowed, so it needs to
            // understand that case as well.

            var numberOfFormalParameters = call.Signature.Parameters.Length;
            var hasEvaluationContext = numberOfFormalParameters > 0 && call.Signature.Parameters[^1].Type == typeof(EvaluationContext);
            var lastParameterIndexFromEnd = hasEvaluationContext && numberOfFormalParameters > 1 ? numberOfFormalParameters - 2 : numberOfFormalParameters - 1;
            var lastParameterIsVariadic = numberOfFormalParameters > 0 && call.Signature.Parameters[lastParameterIndexFromEnd].IsVariadic;
            var lastParameterIsOptional = numberOfFormalParameters > 0 && call.Signature.Parameters[lastParameterIndexFromEnd].IsOptional;

            for(var i = 0; i < call.ParameterValues.Length; i++)
            {
                var parameter = call.ParameterValues[i];
                var currentFormalParameter = (lastParameterIsVariadic && i >= lastParameterIndexFromEnd) ? call.Signature.Parameters[lastParameterIndexFromEnd] : call.Signature.Parameters[i];

                var isPipedValue = i == 0 && call.Pipe != PipeOperator.None && !currentFormalParameter.IsLazyEvaluated;

                // A piped value that is itself a method call is part of the same chain, so it can skip the rest of the chain.

                var value = currentFormalParameter.IsLazyEvaluated
                    ? parameter
                    : isPipedValue && parameter is MethodCallExpression pipedCall
                        ? ExecuteMethodCall(pipedCall, argumentContext, isRootExpression: false)
                        : EvaluateExpression(parameter, argumentContext);

                value = value.UnwrapWith(context.JsonContext.JsonTokenReader);

                if (isPipedValue && (value is SkippedChain || IsSkippedByNullConditional(call, value, context)))
                {
                    return SkippedChain.Instance;
                }

                // External methods take the value that a path, variable or dereferenced property holds, rather than the
                // reference itself. A library method that takes the path as text (e.g. #valueOf) is given its text instead.

                if (!call.Signature.IsSystemMethod && value is RangeVariable variable)
                {
                    value = variable.Value?.ToTypeOf<object>();
                }
                else if (!call.Signature.IsSystemMethod && value is DereferencedPath path)
                {
                    value = UnwrapDereferencedPathForExternalMethod(path);
                }
                else if (!call.Signature.IsSystemMethod && value is QueryPathReference externalPath)
                {
                    value = context.SelectTokenAt(externalPath)?.ToTypeOf<object>();
                }
                else if (value is QueryPathReference textPath && currentFormalParameter.Type == typeof(string))
                {
                    value = textPath.Path;
                }

                if (value is IJsonArray array)
                {
                    if (currentFormalParameter.Type != typeof(IJsonArray) && currentFormalParameter.Type != typeof(object))
                    {
                        if (currentFormalParameter.Type.IsGenericType && currentFormalParameter.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        {
                            var elementType = currentFormalParameter.Type.GetGenericArguments()[0];
                            value = array.Select(x => x.ToTypeOf(elementType)) switch
                            {
                                var x when elementType == typeof(string) => x.Cast<string>(),
                                var x when elementType == typeof(long) => x.Cast<long>(),
                                var x when elementType == typeof(double) => x.Cast<double>(),
                                var x when elementType == typeof(bool) => x.Cast<bool>(),
                                var x when elementType == typeof(IJsonToken) => x.Cast<IJsonToken>(),
                                var x when elementType == typeof(IJsonObject) => x.Cast<IJsonObject>(),
                                var x when elementType == typeof(IJsonArray) => x.Cast<IJsonArray>(),
                                _ => throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToConvertToCurrentParameterEnumerableType, call.Signature, currentFormalParameter.Name)
                            };
                        }
                        else if (currentFormalParameter.Type.IsArray && currentFormalParameter.Type.IsArray)
                        {
                            var elementType = currentFormalParameter.Type.GetElementType();
                            value = array.Select(x => x.ToTypeOf(elementType)).ToArray();
                        }
                        else
                        {
                            throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToConvertJsonArrayToRequiredParameterType, call.Signature.Name, currentFormalParameter.Name);
                        }
                    }
                }
                else if (value is LambdaMethod lambda && currentFormalParameter.Type != typeof(LambdaMethod) && !IsUntypedExternalParameter(call.Signature, currentFormalParameter))
                {
                    if (!currentFormalParameter.IsDelegate)
                    {
                        throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToUseLambdaAsArgumentForNonDelegateParameter, call.Signature.Name, currentFormalParameter.Name);
                    }

                    IJsonToken? ExecuteCustomLambda(object value)
                    {
                        try
                        {
                            var parameters = ((object[])value).Select(x => context.CreateTokenFrom(x)).ToArray();

                            for(var i = 0; i < parameters.Length && i < lambda.Variables.Length; i++)
                            {
                                var variable = new RangeVariable(lambda.Variables[i].Name, parameters[i]);

                                context.Scope.AddOrUpdateVariable(variable);
                            }

                            var evaluationContext = new EvaluationContext(
                                argumentContext.Mode,
                                lambda.Body,
                                context.JsonContext,
                                context.Token,
                                context.Scope,
                                context.Transform);

                            var result = Evaluate(evaluationContext);

                            return result.TransformedToken;
                        }
                        finally
                        {
                            context.Scope.RemoveCurrentVariablesLayer();
                        }
                    }

                    value = LambdaWrapperCache.BuildLambdaDelegate(call.Signature.Name, currentFormalParameter.Type, ExecuteCustomLambda);
                }
                else if (currentFormalParameter.IsDelegate)
                {
                    throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.DelegateParameterRequiresLambdaArgument, call.Signature.Name, currentFormalParameter.Name);
                }

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

                throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.MissingRequiredMethodParameter, call.Signature.Name, nextMissingParameter.Name, nextMissingParameter.Type);
            }
            else if (actualParameterValues.Count > call.Signature.Parameters.Length && !lastParameterIsVariadic)
            {
                throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.MethodCallActualParameterCountExceedsFormalParameterCount, call.Signature.Name, call.Signature.Parameters.Length);
            }

            using var traceScope = call.Signature.Name == nameof(TransformationMethods.Transform) ? context.JsonContext.CreateExecutionTraceScope(actualParameterValues[1]?.ToString()) : context.JsonContext.CreateExecutionTraceScope();

            var parameterStrings = actualParameterValues.Select(v => v?.ToString() ?? "null").ToArray();
            var input = string.Join(",", parameterStrings);

            traceScope.WriteMethodInvocationCheckpoint($"Invoking method '{call.Signature.Name}'");
            traceScope.WriteInputCheckpoint($"Method input '{input}'", parameterStrings);

            var resultValue = InvokeMethod(call.Signature, actualParameterValues, context);

            // An enumerable sequence of JSON tokens is possible to get back from a method call, such as the
            // result of looping over an object or array, and those tokens are orphans that need to be
            // collected and put into the appropriate structure before sending back to the caller.
            // We could also potentially get back a JSON object or array itself, which is already
            // contained and does not need to be packed up into a structure even though they can be enumerated
            // as a sequence of tokens, so we're disallowing the repackaging operation for those types here.

            var isResultJsonObjectOrArray = resultValue is IJsonObject || resultValue is IJsonArray;

            if (!isResultJsonObjectOrArray && typeof(IEnumerable<IJsonToken>).IsAssignableFrom(resultValue?.GetType()))
            {
                // We may have gotten a sequence of either array elements or object properties back
                // with that call so we need to iterate over them and populate the appropriate target structure.

                if (context.Token.CurrentTransformerToken.Type == JsonTokenType.Array)
                {
                    resultValue = context.JsonContext.JsonTokenReader.CreateArrayFrom((IEnumerable<IJsonToken>?)resultValue);
                }
                else if (context.Token.CurrentTransformerToken.Type == JsonTokenType.Object)
                {
                    resultValue = context.JsonContext.JsonTokenReader.CreateObjectFrom((IEnumerable<IJsonToken>?)resultValue);
                }
            }
            else if (!isResultJsonObjectOrArray && IsEnumerableAsPrimitiveType(resultValue, out var sequence))
            {
                var tokenSequence = sequence.Select(x => context.JsonContext.JsonTokenReader.CreateTokenFrom(x));

                resultValue = context.JsonContext.JsonTokenReader.CreateArrayFrom(tokenSequence);
            }
            
            var resultString = resultValue?.ToString();

            traceScope.WriteOutputCheckpoint($"Method invocation completed with '{resultString}'", resultString);

            if (context.Mode == EvaluationMode.PropertyName)
            {
                // The method may have been a value generator, meaning that evaluating the property name will
                // also cause the value for that property to be generated (e.g. the loop method) but if we're
                // taking a look at a non-generator property then this needs to be flagged when it's sent back so that
                // the transformer follows up with evaluating the value as well and doesn't just assume that
                // it's already happened.

                if (call.Signature.IsValueGenerator)
                {
                    return new EvaluationResult(context.Token.PropertyName, context.Token.ResolvedPropertyName, (IJsonToken)resultValue, rangeVariable: context.Token.ParentRangeVariable ?? call.GeneratedVariable);
                }

                // We already know that we need to also evaluate the value in addition to the property,
                // but since we're on the property name side we need to figure out what the new name of this
                // property is going to be and send that back out with the result so it gets named appropriately.

                var resolvedPropertyName = resultValue switch
                {
                    IJsonProperty property => property.PropertyName,
                    IJsonValue value => value.ToTypeOf<string>(),
                    _ => throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToResolveNewPropertyNameForUnsupportedResultOfType, resultValue?.GetType())
                };

                return new EvaluationResult(context.Token.PropertyName, resolvedPropertyName, context.Token.CurrentTransformerToken, isValuePendingEvaluation: true);
            }
            else if (context.Mode == EvaluationMode.PropertyValue)
            {
                if (call is IndexOrSliceMethodResultExpression indexOrSlice)
                {
                    return IndexOrSliceWithRange(indexOrSlice.ResultRange, (IJsonToken)resultValue, context, isRootExpression);
                }

                return resultValue;
            }

            throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToApplyMethodResultsWithUnknownEvaluationMode, context.Mode);
        }

        private static bool IsEnumerableAsPrimitiveType(object? obj, out IEnumerable<object> result)
        {
            if (obj is null)
            {
                result = Array.Empty<object>();
                return false;
            }

            var type = obj.GetType();

            var isAssignable = typeof(IEnumerable<string>).IsAssignableFrom(type) ||
                               typeof(IEnumerable<long>).IsAssignableFrom(type) ||
                               typeof(IEnumerable<double>).IsAssignableFrom(type) ||
                               typeof(IEnumerable<bool>).IsAssignableFrom(type);
            
            if (!isAssignable)
            {
                result = Array.Empty<object>();
                return false;
            }

            result = ((IEnumerable)obj).OfType<object>();

            return true;
        }

        /// <summary>
        /// Determines whether an external method's parameter is untyped (an object, or a params array of them), which
        /// accepts a lambda as the <see cref="LambdaMethod"/> itself. This lets a method take any arguments, such as a
        /// stand-in for a method that isn't available, without knowing which of them are lambdas.
        /// </summary>
        private static bool IsUntypedExternalParameter(MethodSignature signature, MethodParameter parameter)
        {
            var type = parameter.IsVariadic && parameter.Type.IsArray ? parameter.Type.GetElementType() : parameter.Type;

            return !signature.IsSystemMethod && type == typeof(object);
        }

        /// <summary>
        /// Stands in for the result of a chain of piped method calls that a null-conditional operator ('?.' or '?->') cut short,
        /// so that the rest of the chain is skipped too. The chain as a whole then gives null (see <see cref="EndChain"/>).
        /// </summary>
        private sealed class SkippedChain
        {
            public static readonly SkippedChain Instance = new SkippedChain();
        }

        private static object? EndChain(object? result)
        {
            return result is SkippedChain ? default : result;
        }

        /// <summary>
        /// Gets whether a value piped into a method skips it (and the rest of its chain), which happens when a null-safe
        /// dereference skipped its chain (e.g. "@x?.name->#trim()" when '@x' is null) or a null-conditional pipe finds
        /// null or nothing (e.g. "@x.name?->#trim()" when 'name' is null or missing).
        /// </summary>
        private static bool IsSkippedByNullConditional(MethodCallExpression call, object? value, EvaluationContext context)
        {
            static bool IsNullOrMissing(DereferencedPath path) => path.MissingPaths.Length > 0 || ValueResolutionExtensions.IsNull(path.ObtainableToken);

            if (value is DereferencedPath { IsNullSafe: true })
            {
                return true;
            }

            if (call.Pipe != PipeOperator.NullConditionalPipe)
            {
                return false;
            }

            return value switch
            {
                null => true,
                IJsonToken token => ValueResolutionExtensions.IsNull(token),
                RangeVariable variable => ValueResolutionExtensions.IsNull(variable.Value),
                DereferencedPath path => IsNullOrMissing(path),
                QueryPathReference path => ValueResolutionExtensions.IsNull(context.SelectTokenAt(path)),
                _ => false
            };
        }

        private static object? UnwrapDereferencedPathForExternalMethod(DereferencedPath path)
        {
            // A missing property, or a chain skipped by a null-safe dereference, gives null.

            return path.MissingPaths.Length > 0 ? null : path.ObtainableToken?.ToTypeOf<object>();
        }

        private static object? InvokeMethod(MethodSignature method, IEnumerable<object?> actualParameterValues, EvaluationContext context)
        {
            try
            {
                if (method.CallType == CallType.Static)
                {
                    context.JsonContext.WriteInfoFor<ExpressionEvaluator>($"Attempting to invoke static method '{method.AssemblyQualifiedTypeName}.{method.Name}'");

                    var type = Type.GetType(method.AssemblyQualifiedTypeName);

                    return type
                        .ThrowIfNull(nameof(type), $"Unable to locate type '{method.AssemblyQualifiedTypeName}' for invocation of method '{method.Name}'")
                        .AndGet(x => x.GetMethod(method.Name, BindingFlags.Public | BindingFlags.Static))
                        .ThrowIfNull(nameof(method.Name), $"Unable to locate static method '{method.Name}' within type '{method.AssemblyQualifiedTypeName}'")
                        .AndFinally(x => x.Value!.Invoke(null, actualParameterValues.ToArray()));
                }
                else if (method.CallType == CallType.Instance)
                {
                    context.JsonContext.WriteInfoFor<ExpressionEvaluator>($"Attempting to invoke instance method '{method.Name}'");

                    if (context.JsonContext.MethodContext is null)
                    {
                        throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToInvokeInstanceMethodWithoutMethodContext, method.Name);
                    }

                    var methodInfo = context.JsonContext.MethodContext.GetType().GetMethod(method.Name, BindingFlags.Public | BindingFlags.Instance);

                    if (methodInfo is null)
                    {
                        throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToLocateInstanceMethodWithProvidedMethodContext, method.Name, context.JsonContext.MethodContext.GetType().FullName);
                    }

                    return methodInfo.Invoke(context.JsonContext.MethodContext, actualParameterValues.ToArray());
                }

                else if (method.CallType == CallType.Delegate && method.MethodDelegate != null)
                {
                    context.JsonContext.WriteInfoFor<ExpressionEvaluator>($"Attempting to invoke delegate method '{method.Name}'");

                    return method.MethodDelegate.DynamicInvoke(actualParameterValues.ToArray());
                }

                throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.UnableToInvokeMethodWithUnknownCallType, method.CallType);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is JoltException jex)
            {
                throw context.CreateExecutionErrorFor<ExpressionEvaluator>(ExceptionCode.ExternalMethodInvocationCausedAnException, jex, method.Name);
            }
        }
    }
}
