using Jolt.Exceptions;
using Jolt.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jolt.Evaluation
{
    /// <summary>
    /// Represents the rules for where statement methods may appear. A statement is only valid as the root of an entry
    /// in a 'using' block or as one of the statements of another statement (i.e. '#when'), and it must be a direct call
    /// rather than a piped one. Its other parameters are either paths that dereference the 'using' variable or property
    /// value expressions, which may not contain statements themselves.
    /// </summary>
    internal static class StatementRules
    {
        /// <summary>
        /// Gets the ways that the given expression breaks the rules for being an entry in a 'using' block.
        /// </summary>
        /// <param name="expression">The expression to check.</param>
        /// <param name="usingVariableName">The name of the variable that the 'using' block operates on.</param>
        /// <param name="isVariableDeclared">Determines whether a variable name is in scope. An undeclared variable is not treated as a violation here, as that's a separate problem to report.</param>
        /// <returns>A sequence of violations, empty if the expression is a valid statement.</returns>
        public static IEnumerable<StatementViolation> GetViolationsFor(Expression expression, string usingVariableName, Func<string, bool> isVariableDeclared)
        {
            return GetViolationsFor(expression, usingVariableName, isVariableDeclared, isWithinStatement: false);
        }

        /// <summary>
        /// Gets the outermost statement method calls within the given expression, however deeply they're nested.
        /// The contents of a statement aren't included, since it's already a problem for the statement to be there.
        /// </summary>
        /// <param name="expression">The expression to look through.</param>
        /// <returns>A sequence of statement method calls, in the order that they were found.</returns>
        public static IEnumerable<MethodCallExpression> FindStatementsWithin(Expression? expression)
        {
            if (expression is MethodCallExpression call && call.Signature.IsAllowedAsStatement)
            {
                yield return call;
                yield break;
            }

            foreach (var nested in GetChildrenOf(expression).SelectMany(FindStatementsWithin))
            {
                yield return nested;
            }
        }

        private static IEnumerable<StatementViolation> GetViolationsFor(Expression expression, string usingVariableName, Func<string, bool> isVariableDeclared, bool isWithinStatement)
        {
            if (!(expression is MethodCallExpression call))
            {
                var code = isWithinStatement ? ExceptionCode.AttemptedToUseNonMethodExpressionWithinWhenLibraryCall : ExceptionCode.UnableToUseNonStatementExpressionWithinStatementBlock;

                yield return new StatementViolation(code, expression.Span, expression.GetType().Name);
                yield break;
            }

            if (!call.Signature.IsAllowedAsStatement)
            {
                var code = isWithinStatement ? ExceptionCode.AttemptedToUseNonStatementMethodWithinWhenLibraryCall : ExceptionCode.UnableToUseMethodWithinStatementBlock;

                yield return new StatementViolation(code, call.Span, call.Signature.Alias);
                yield break;
            }

            if (call.Pipe != PipeOperator.None)
            {
                yield return new StatementViolation(ExceptionCode.UnableToPipeIntoStatementMethod, call.Span, call.Signature.Alias);
                yield break;
            }

            if (call is IndexOrSliceMethodResultExpression || call.GeneratedName != null || call.GeneratedVariable != null)
            {
                yield return new StatementViolation(ExceptionCode.UnableToUseNonStatementExpressionWithinStatementBlock, call.Span, call.GetType().Name);
                yield break;
            }

            var formalParameters = call.Signature.Parameters.Where(x => x.Type != typeof(EvaluationContext)).ToArray();
            var takesStatements = formalParameters.Any(IsStatementParameter);
            var hasStatements = false;

            for (var i = 0; i < call.ParameterValues.Length; i++)
            {
                var parameter = call.ParameterValues[i];

                // Too many parameters is reported on its own when checking the method call's arguments.

                var formalParameter = i < formalParameters.Length ? formalParameters[i] : formalParameters.LastOrDefault(x => x.IsVariadic);

                if (formalParameter != null && IsStatementParameter(formalParameter))
                {
                    hasStatements = true;

                    foreach (var violation in GetViolationsFor(parameter, usingVariableName, isVariableDeclared, isWithinStatement: true))
                    {
                        yield return violation;
                    }
                }
                else if (formalParameter?.Type == typeof(DereferencedPath))
                {
                    foreach (var violation in GetPathViolationsFor(call, parameter, usingVariableName, isVariableDeclared))
                    {
                        yield return violation;
                    }
                }
                else
                {
                    foreach (var nested in FindStatementsWithin(parameter))
                    {
                        yield return new StatementViolation(ExceptionCode.UnableToUseMethodWithinNonRootStatementBlock, nested.Span, nested.Signature.Alias);
                    }
                }
            }

            if (takesStatements && !hasStatements)
            {
                yield return new StatementViolation(ExceptionCode.UnableToUseStatementMethodWithoutStatements, call.Span, call.Signature.Alias);
            }
        }

        private static IEnumerable<StatementViolation> GetPathViolationsFor(MethodCallExpression call, Expression path, string usingVariableName, Func<string, bool> isVariableDeclared)
        {
            if (!(path is PropertyDereferenceExpression dereference) || dereference.Variable.ProvidesNullSafeAccess || dereference.DereferenceChain.Any(x => x.IsNullSafe))
            {
                yield return new StatementViolation(ExceptionCode.UnableToUseStatementPathThatIsNotDereferenceOfUsingVariable, path.Span ?? call.Span, call.Signature.Alias, usingVariableName);
                yield break;
            }

            if (dereference.Variable.Name != usingVariableName && isVariableDeclared(dereference.Variable.Name))
            {
                yield return new StatementViolation(ExceptionCode.AttemptedToIndirectlyModifyVariableWithinUsingBlock, dereference.Variable.Span ?? call.Span, dereference.Variable.Name);
            }
        }

        private static bool IsStatementParameter(Parsing.MethodParameter parameter) => parameter.IsLazyEvaluated && parameter.IsVariadic;

        private static IEnumerable<Expression?> GetChildrenOf(Expression? expression)
        {
            switch (expression)
            {
                case IndexOrSliceMethodResultExpression indexOrSlice:
                    return indexOrSlice.ParameterValues.Append<Expression?>(indexOrSlice.ResultRange);

                case MethodCallExpression call:
                    return call.ParameterValues;

                case BinaryExpression binary:
                    return new[] { binary.Left, binary.Right };

                case LogicalNotExpression not:
                    return new[] { not.Operand };

                case ArrayLiteralExpression array:
                    return array.Elements;

                case ObjectLiteralExpression obj:
                    return obj.Properties.Select(x => x.PropertyValue);

                case SlicedVariableExpression sliced:
                    return new Expression[] { sliced.Range };

                case LambdaMethodExpression lambda:
                    return new[] { lambda.Body };

                case RangeExpression range:
                    return new Expression[] { range.StartIndex, range.EndIndex };

                case RangeIndexExpression index:
                    return new[] { index.Index };

                case EnumerateAsVariableExpression enumerate:
                    return new[] { enumerate.EnumerationSource };

                default:
                    return Enumerable.Empty<Expression?>();
            }
        }
    }

    /// <summary>
    /// Represents a way that an expression breaks the rules for where statement methods may appear.
    /// </summary>
    internal sealed class StatementViolation
    {
        public ExceptionCode Code { get; }
        public Parsing.ExpressionSpan? Span { get; }
        public object[] Parameters { get; }

        public StatementViolation(ExceptionCode code, Parsing.ExpressionSpan? span, params object[] parameters)
        {
            Code = code;
            Span = span;
            Parameters = parameters;
        }
    }
}
