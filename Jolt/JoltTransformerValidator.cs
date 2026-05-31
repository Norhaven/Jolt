using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Expressions;
using Jolt.Structure;
using System;
using System.Collections.Generic;

namespace Jolt
{
    /// <summary>
    /// Represents a way of using static analysis to validate the correctness of a given JSON transformer without needing actual data to transform.
    /// </summary>
    /// <typeparam name="TContext">The JSON context used in this validation.</typeparam>
    public sealed class JoltTransformerValidator<TContext> where TContext : IJsonContext
    {
        private readonly TContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="JoltTransformerValidator{TContext}"/> class with the specified JSON context.
        /// </summary>
        /// <param name="context">The JSON context used in this validation.</param>
        public JoltTransformerValidator(TContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Validates the given JSON transformer and returns a sequence of any issues found during validation.
        /// </summary>
        /// <param name="jsonTransformer">The JSON transformer.</param>
        /// <returns>A sequence of validation issues, empty if no issues were found.</returns>
        public IEnumerable<ValidationIssue> Validate(IJsonToken jsonTransformer)
        {
            var rootScope = ValidationScope.Empty;

            return ValidateValue(jsonTransformer, rootScope);
        }

        private IEnumerable<ValidationIssue> ValidateObject(IJsonObject obj, ValidationScope scope)
        {
            var runningScope = scope;

            foreach (var property in obj)
            {
                foreach (var issue in ValidateExpressionProperty(property, runningScope))
                    yield return issue;

                // After each property is validated, update the running scope with any sibling-visible
                // variable it declares so subsequent properties can reference them.

                runningScope = CollectSiblingScope(property, runningScope);
            }
        }

        private IEnumerable<ValidationIssue> ValidateExpressionProperty(IJsonProperty property, ValidationScope scope)
        {
            var isPropertyNameAnExpression = _context.TokenReader.StartsWithMethodCallOrOpenParenthesesOrRangeVariableOrOpenSquareBracketOrLogicalNot(property.PropertyName);
            var valueAlreadyHandled = false;

            if (isPropertyNameAnExpression)
            {
                if (!TryReadAndParseExpression(property.FullPath, property.PropertyName, EvaluationMode.PropertyName, out var nameExpression, out var nameIssue))
                {
                    yield return nameIssue!;
                }
                else if (nameExpression != null)
                {
                    // A standalone range variable on the property name side is a variable declaration
                    // (@varName), not a use — skip the scope check for the name expression itself.

                    if (!(nameExpression is RangeVariableExpression))
                    {
                        foreach (var issue in ValidateExpression(nameExpression, EvaluationMode.PropertyName, scope))
                            yield return issue;
                    }

                    // Value-generating methods (#foreach, #using, #includeIf, …) own the property value:
                    // they may introduce a child scope for loop or alias variables, and the property value
                    // (template array / statement list / conditional object) must be validated inside that scope.

                    if (nameExpression is MethodCallExpression nameMethodCall && nameMethodCall.Signature.IsValueGenerator)
                    {
                        var childScope = BuildChildScope(nameMethodCall, scope);

                        if (property.Value != null)
                        {
                            foreach (var issue in ValidateValue(property.Value, childScope))
                                yield return issue;
                        }

                        valueAlreadyHandled = true;
                    }
                }
            }

            // For all other cases (plain nested objects/arrays, string expressions, or non-value-generating
            // property names) let ValidateValue recurse into the property value as appropriate.

            if (!valueAlreadyHandled && property.Value != null)
            {
                foreach (var issue in ValidateValue(property.Value, scope))
                    yield return issue;
            }
        }

        private static ValidationScope BuildChildScope(MethodCallExpression methodCall, ValidationScope scope)
        {
            // Builds the child scope for a value-generator method call by extracting loop or alias variables
            // from its parameters.  Covers @x (and optionally @i) from #foreach and @x from #using.

            var names = new List<string>();

            foreach (var parameter in methodCall.ParameterValues)
            {
                switch (parameter)
                {
                    case EnumerateAsVariableExpression enumerate:
                        names.Add(enumerate.Variable.Name);
                        if (enumerate.Variable is RangeVariablePairExpression pair)
                            names.Add(pair.SecondVariable.Name);
                        break;

                    case VariableAliasExpression alias:
                        names.Add(alias.AliasVariable.Name);
                        break;
                }
            }

            return scope.With(names);
        }

        // Checks whether a property's name declares a sibling-visible variable and, if so, adds it to the
        // scope.  Two cases: a bare @varName declaration, or a value-generator method whose output is
        // assigned to a variable via "into @varName".

        private ValidationScope CollectSiblingScope(IJsonProperty property, ValidationScope scope)
        {
            var name = property.PropertyName;

            // Fast path: @varName declarations need no parsing — the name IS the variable.

            if (name.Length > 1 && name[0] == '@')
                return scope.With(name);

            if (!_context.TokenReader.StartsWithMethodCallOrOpenParenthesesOrRangeVariableOrOpenSquareBracketOrLogicalNot(name))
                return scope;

            // Check for "into @varName" on a value-generator method (e.g., #foreach(...) into @temp).

            if (TryReadAndParseExpression(property.FullPath, name, EvaluationMode.PropertyName, out var expr, out _)
                && expr is MethodCallExpression methodCall
                && methodCall.GeneratedVariable != null)
            {
                return scope.With(methodCall.GeneratedVariable.Name);
            }

            return scope;
        }

        private bool IsExpressionIntroducingScopedVariables(Expression expression, ValidationScope scope, EvaluationMode mode) =>
            expression is MethodCallExpression methodCall && IsMethodCallExpressionIntroducingScopedVariables(methodCall, scope, mode)
            || expression is EnumerateAsVariableExpression
            || expression is LambdaMethodExpression
            || expression is RangeVariableExpression && mode == EvaluationMode.PropertyName;

        private bool IsMethodCallExpressionIntroducingScopedVariables(MethodCallExpression methodCall, ValidationScope scope, EvaluationMode mode)
        {
            if (methodCall.GeneratedVariable != null)
            {
                return true;
            }

            foreach(var parameter in methodCall.ParameterValues)
            {
                if (IsExpressionIntroducingScopedVariables(parameter, scope, mode))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryReadAndParseExpression(string expressionPath, string expressionString, EvaluationMode mode, out Expression? expression, out ValidationIssue? issue)
        {
            expression = default;
            issue = default;

            try
            {
                var expressionTokens = _context.TokenReader.ReadToEnd(expressionString, mode);

                if (_context.ExpressionParser.TryParseExpression(expressionTokens, _context, out expression))
                {
                    return true;
                }
            }
            catch(JoltException ex)
            {
                issue = new ValidationIssue(ValidationIssueType.SyntaxError, ex.Code, $"Error parsing expression at transformer path '{expressionPath}' with expression '{expressionString}': {ex.Message}", expressionPath, expressionString);
                return false;
            }

            return true;
        }

        private IEnumerable<ValidationIssue> ValidateValue(IJsonToken token, ValidationScope scope)
        {
            switch (token.Type)
            {
                case JsonTokenType.Object:
                    foreach (var issue in ValidateObject(token.AsObject(), scope))
                        yield return issue;
                    break;

                case JsonTokenType.Array:
                    foreach (var issue in ValidateArray(token.AsArray(), scope))
                        yield return issue;
                    break;

                case JsonTokenType.Value:
                {
                    var value = token.AsValue();

                    if (value.ValueType == JsonValueType.String)
                    {
                        var str = value.ToTypeOf<string>();

                        if (str != null && _context.TokenReader.StartsWithMethodCallOrOpenParenthesesOrRangeVariableOrOpenSquareBracketOrLogicalNot(str))
                        {
                            if (!TryReadAndParseExpression(token.FullPath, str, EvaluationMode.PropertyValue, out var expr, out var issue))
                                yield return issue!;
                            else if (expr != null)
                                foreach (var validationIssue in ValidateExpression(expr, EvaluationMode.PropertyValue, scope))
                                    yield return validationIssue;
                        }
                    }

                    break;
                }
            }
        }

        private IEnumerable<ValidationIssue> ValidateArray(IJsonArray array, ValidationScope scope)
        {
            foreach (var element in array)
                foreach (var issue in ValidateValue(element, scope))
                    yield return issue;
        }

        private IEnumerable<ValidationIssue> ValidateExpression(Expression expression, EvaluationMode mode, ValidationScope scope)
        {
            switch (expression)
            {
                case LiteralExpression _:
                case PathExpression _:
                case NullCoalescingExpression _:
                    break;

                // IndexOrSliceMethodResultExpression must precede MethodCallExpression since it extends it.

                case IndexOrSliceMethodResultExpression indexOrSlice:
                    foreach (var issue in ValidateMethodCall(indexOrSlice, mode, scope))
                        yield return issue;
                    foreach (var issue in ValidateExpression(indexOrSlice.ResultRange, mode, scope))
                        yield return issue;
                    break;

                case MethodCallExpression methodCall:
                    foreach (var issue in ValidateMethodCall(methodCall, mode, scope))
                        yield return issue;
                    break;

                case BinaryExpression binary:
                    foreach (var issue in ValidateExpression(binary.Left, mode, scope))
                        yield return issue;
                    foreach (var issue in ValidateExpression(binary.Right, mode, scope))
                        yield return issue;
                    break;

                case LogicalNotExpression not:
                    foreach (var issue in ValidateExpression(not.Operand, mode, scope))
                        yield return issue;
                    break;

                case ArrayLiteralExpression arrayLiteral:
                    foreach (var element in arrayLiteral.Elements)
                        foreach (var issue in ValidateExpression(element, mode, scope))
                            yield return issue;
                    break;

                // SlicedVariableExpression and PropertyDereferenceExpression must precede RangeVariableExpression
                // since they both extend it.

                case SlicedVariableExpression sliced:
                    if (!scope.IsVariableDeclared(sliced.Variable.Name))
                        yield return UndeclaredVariableIssue(sliced.Variable.Name);
                    foreach (var issue in ValidateExpression(sliced.Range, mode, scope))
                        yield return issue;
                    break;

                case PropertyDereferenceExpression dereference:
                    if (!scope.IsVariableDeclared(dereference.Variable.Name))
                        yield return UndeclaredVariableIssue(dereference.Variable.Name);
                    break;

                // RangeVariablePairExpression also extends RangeVariableExpression; its .Name is the first variable.

                case RangeVariableExpression variable:
                    if (!scope.IsVariableDeclared(variable.Name))
                        yield return UndeclaredVariableIssue(variable.Name);
                    break;

                case EnumerateAsVariableExpression enumerate:

                    // The variable is being introduced here, not consumed — only validate the source.

                    foreach (var issue in ValidateExpression(enumerate.EnumerationSource, mode, scope))
                        yield return issue;
                    break;

                case VariableAliasExpression alias:

                    // The alias variable is being introduced; only the source variable needs to be in scope.

                    if (!alias.IsSourceFromPath && alias.SourceVariable != null && !scope.IsVariableDeclared(alias.SourceVariable.Name))
                        yield return UndeclaredVariableIssue(alias.SourceVariable.Name);
                    break;

                case LambdaMethodExpression lambda:

                    // Lambda variable is scoped to the body only — create a child scope for it.

                    var lambdaScope = scope.With(lambda.Variable.Name);
                    foreach (var issue in ValidateExpression(lambda.Body, mode, lambdaScope))
                        yield return issue;
                    break;

                case RangeExpression range:
                    foreach (var issue in ValidateExpression(range.StartIndex.Index, mode, scope))
                        yield return issue;
                    foreach (var issue in ValidateExpression(range.EndIndex.Index, mode, scope))
                        yield return issue;
                    break;

                case RangeIndexExpression rangeIndex:
                    foreach (var issue in ValidateExpression(rangeIndex.Index, mode, scope))
                        yield return issue;
                    break;
            }
        }

        private IEnumerable<ValidationIssue> ValidateMethodCall(MethodCallExpression methodCall, EvaluationMode mode, ValidationScope scope)
        {
            var signature = methodCall.Signature;

            if (mode == EvaluationMode.PropertyName && !signature.IsAllowedAsPropertyName)
            {
                yield return new ValidationIssue(
                    ValidationIssueType.InvalidMethodContext,
                    ExceptionCode.UnableToUseMethodWithinPropertyName,
                    $"Method '{signature.Alias}' is not valid in a property name context.",
                    null,
                    signature.Alias);
            }

            // Statement-only methods are exempt from the property-value check because they legitimately
            // appear in property-value position inside a #using block's statement array.

            if (mode == EvaluationMode.PropertyValue && !signature.IsAllowedAsPropertyValue && !signature.IsAllowedAsStatement)
            {
                yield return new ValidationIssue(
                    ValidationIssueType.InvalidMethodContext,
                    ExceptionCode.UnableToUseMethodWithinPropertyValue,
                    $"Method '{signature.Alias}' is not valid in a property value context.",
                    null,
                    signature.Alias);
            }

            if (signature.IsUnsafe)
            {
                yield return new ValidationIssue(
                    ValidationIssueType.UnsafeMethodUsage,
                    ExceptionCode.UnsafeMethodCallNotAllowed,
                    $"Method '{signature.Alias}' is unsafe. Enable unsafe evaluations via JoltOptions.WithUnsafeAllowed() if this is intentional.",
                    null,
                    signature.Alias);
            }

            // Arity check: mirrors the parameter-counting logic in ExpressionEvaluator.
            // System methods append an EvaluationContext as the final formal parameter; exclude it from
            // the user-visible count. The last user parameter may be variadic (0 or more) or optional.

            var formalCount = signature.Parameters.Length;
            var hasEvaluationContext = formalCount > 0 && signature.Parameters[^1].Type == typeof(EvaluationContext);
            var userParamCount = hasEvaluationContext ? formalCount - 1 : formalCount;
            var actualCount = methodCall.ParameterValues.Length;

            if (userParamCount == 0)
            {
                if (actualCount > 0)
                {
                    yield return new ValidationIssue(
                        ValidationIssueType.ArgumentCountMismatch,
                        ExceptionCode.MethodCallActualParameterCountExceedsFormalParameterCount,
                        $"Method '{signature.Alias}' expects no arguments but received {actualCount}.",
                        null,
                        signature.Alias);
                }
            }
            else
            {
                var lastUserParam = signature.Parameters[userParamCount - 1];
                var lastIsVariadic = lastUserParam.IsVariadic;
                var lastIsOptional = lastUserParam.IsOptional;

                var minArgs = (lastIsVariadic || lastIsOptional) ? userParamCount - 1 : userParamCount;
                var maxArgs = lastIsVariadic ? int.MaxValue : userParamCount;

                if (actualCount < minArgs)
                {
                    yield return new ValidationIssue(
                        ValidationIssueType.ArgumentCountMismatch,
                        ExceptionCode.MissingRequiredMethodParameter,
                        $"Method '{signature.Alias}' expects {(minArgs == maxArgs ? $"{minArgs}" : $"{minArgs}–{(lastIsVariadic ? "∞" : maxArgs.ToString())}")} argument(s) but received {actualCount}.",
                        null,
                        signature.Alias);
                }

                if (!lastIsVariadic && actualCount > maxArgs)
                {
                    yield return new ValidationIssue(
                        ValidationIssueType.ArgumentCountMismatch,
                        ExceptionCode.MethodCallActualParameterCountExceedsFormalParameterCount,
                        $"Method '{signature.Alias}' expects {(minArgs == maxArgs ? $"{minArgs}" : $"{minArgs}–{(lastIsVariadic ? "∞" : maxArgs.ToString())}")} argument(s) but received {actualCount}.",
                        null,
                        signature.Alias);
                }
            }

            foreach (var parameter in methodCall.ParameterValues)
            {
                foreach (var issue in ValidateExpression(parameter, mode, scope))
                {
                    yield return issue;
                }
            }
        }

        private static ValidationIssue UndeclaredVariableIssue(string variableName) =>
            new ValidationIssue(
                ValidationIssueType.UndeclaredVariable,
                ExceptionCode.AttemptedToUseUndeclaredVariable,
                $"Range variable '{variableName}' is referenced but has not been declared in the current scope.",
                null,
                variableName);
    }
}
