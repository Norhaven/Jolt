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
    internal sealed class StatementMethods
    {
        [JoltLibraryMethod("when")]
        [MethodIsValidOn(LibraryMethodTarget.StatementBlock)]
        public static EvaluationResult? When(object? result, [LazyEvaluation][VariadicEvaluation] object[]? expressions, EvaluationContext context)
        {
            var resolved = context.ResolveValueOrNullOf<object>(result);

            if (resolved is null || (resolved is IJsonToken nullable && ValueResolutionExtensions.IsNull(nullable)))
            {
                throw context.CreateExecutionErrorFor<StatementMethods>(ExceptionCode.UnableToEvaluateWhenConditionWithNullValue);
            }

            var isTrue = resolved switch
            {
                bool value => value,
                IJsonToken token => token.Type == JsonTokenType.Value && token.AsValue().ValueType == JsonValueType.Boolean && token.AsValue().ToTypeOf<bool>(),
                _ => false
            };

            if (!isTrue || expressions is null)
            {
                return default;
            }

            EvaluationResult? currentResult = default;

            // These have already been verified as statements by StatementRules, regardless of the condition.

            foreach (var expression in expressions)
            {
                var evaluationContext = new EvaluationContext(
                    context.Mode,
                    (MethodCallExpression)expression,
                    context.JsonContext,
                    context.Token,
                    context.Scope,
                    context.Transform
                );

                currentResult = context.JsonContext.ExpressionEvaluator.Evaluate(evaluationContext);
            }

            return currentResult;
        }

        [JoltLibraryMethod("removeAt")]
        [MethodIsValidOn(LibraryMethodTarget.StatementBlock)]
        public static IJsonToken? RemoveAt(DereferencedPath path, EvaluationContext context)
        {
            // A property containing a JSON null exists, even though the path couldn't get a token for it.

            if (path.MissingPaths.Length == 1 && path.ObtainableToken is IJsonObject container && container.HasProperty(path.MissingPaths[0]))
            {
                return container.Remove(path.MissingPaths[0]);
            }

            if (path.MissingPaths.Length > 0)
            {
                var missing = path.MissingPaths.Join('.');
                throw context.CreateExecutionErrorFor<StatementMethods>(ExceptionCode.AttemptedToDereferenceMissingPath, missing, path.ObtainableToken.PropertyName);
            }

            var token = path.ObtainableToken;

            if (token.Parent is IJsonObject obj)
            {
                obj.Remove(token.PropertyName);
            }
            else if (token.Parent is IJsonProperty property)
            {
                ((IJsonObject)property.Parent).Remove(token.PropertyName);
            }
            else
            {
                throw context.CreateExecutionErrorFor<StatementMethods>(ExceptionCode.UnableToRemoveNodeFromNonObjectParent, token.PropertyName);
            }

            return token;
        }

        [JoltLibraryMethod("setAt")]
        [MethodIsValidOn(LibraryMethodTarget.StatementBlock)]
        public static IJsonToken? SetAt(DereferencedPath path, object? newValue, EvaluationContext context)
        {
            var token = path.ObtainableToken;

            var actualNewValue = newValue switch
            {
                DereferencedPath pathValue when pathValue.MissingPaths.Length == 0 && pathValue.ObtainableToken != null => pathValue.ObtainableToken,
                DereferencedPath _ => context.CreateTokenFrom(null),
                RangeVariable variable => context.CreateTokenFrom(variable.Value),
                QueryPathReference pathValue => context.SelectTokenAt(pathValue) ?? context.CreateTokenFrom(null),
                object obj => context.CreateTokenFrom(obj),
                null => context.CreateTokenFrom(null)
            };

            // When the entire path already exists, the token is the node being replaced rather than
            // an object to add the missing part of the path to, so we need to set it on its parent.

            if (path.MissingPaths.Length == 0)
            {
                var parent = token.Parent switch
                {
                    IJsonObject obj => obj,
                    IJsonProperty property => property.Parent as IJsonObject,
                    _ => default
                };

                if (parent is null)
                {
                    throw context.CreateExecutionErrorFor<StatementMethods>(ExceptionCode.UnableToSetPropertyOnNonObjectReference, token.PropertyName);
                }

                parent[token.PropertyName] = actualNewValue;

                return parent;
            }

            var current = token as IJsonObject;

            if (current is null)
            {
                throw context.CreateExecutionErrorFor<StatementMethods>(ExceptionCode.UnableToSetPropertyOnNonObjectReference, token.PropertyName);
            }

            var missingPath = path.MissingPaths.Join('.');

            current.AddAtPath(missingPath, actualNewValue);

            return token;
        }

        [JoltLibraryMethod("using", isValueGenerator: true)]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName)]
        public static IJsonToken? Using(VariableAlias variable, EvaluationContext context)
        {
            var token = context.Token.CurrentTransformerToken;

            if (!token.Type.IsAnyOf(JsonTokenType.Array))
            {
                throw context.CreateExecutionErrorFor<StatementMethods>(ExceptionCode.UnableToPerformUsingLibraryCallOnNonArrayToken, token.Type);
            }

            var closestViableSourceToken = variable.Source switch
            {
                QueryPathReference path => context.SelectTokenAt(path),
                RangeVariable rangeVariable => rangeVariable.Value,
                _ => throw context.CreateExecutionErrorFor<StatementMethods>(ExceptionCode.UnableToPerformUsingLibraryCallDueToInvalidParameter, variable.Source)
            };

            if (closestViableSourceToken?.Type != JsonTokenType.Object)
            {
                throw context.CreateExecutionErrorFor<StatementMethods>(ExceptionCode.UnableToPerformUsingLibraryCallOnNonObjectToken, closestViableSourceToken?.Type);
            }

            var statements = token switch
            {
                IJsonArray array => array.Copy().AsArray(),
                _ => throw new ArgumentOutOfRangeException(nameof(variable), $"Unable to locate array containing statements for 'using' block")
            };

            token.Clear();

            var loopVariable = new RangeVariable(variable.Variable.Name, closestViableSourceToken.Copy());

            context.Scope.AddOrUpdateVariable(loopVariable);

            try
            {
                foreach (var statement in statements)
                {
                    var currentEvaluationToken = new EvaluationToken(context.Token.PropertyName, context.Token.ResolvedPropertyName, token, statement, default, true, statementVariableName: loopVariable.Name);

                    context.Transform(currentEvaluationToken, context.Scope);
                }

                return loopVariable.Value;
            }
            finally
            {
                context.Scope.RemoveCurrentVariablesLayer();
            }
        }
    }
}
