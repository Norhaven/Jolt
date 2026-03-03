using Jolt.Evaluation;
using Jolt.Exceptions;
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
        [JoltLibraryMethod("removeAt")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue | LibraryMethodTarget.StatementBlock)]
        public static IJsonToken? RemoveAt(DereferencedPath path, EvaluationContext context)
        {
            // Ensure that non-scoped variables outside of the 'using' block aren't able to be
            // modified just because we're in statement mode for some other use.

            if (!context.Scope.ContainsVariable(path.SourceVariable.Name, onlyCheckTopLayer: true))
            {
                throw context.CreateExecutionErrorFor<StatementMethods>(ExceptionCode.AttemptedToIndirectlyModifyVariableWithinUsingBlock, path.SourceVariable.Name);
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
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue | LibraryMethodTarget.StatementBlock)]
        public static IJsonToken? SetAt(DereferencedPath path, object? newValue, EvaluationContext context)
        {
            // Ensure that non-scoped variables outside of the 'using' block aren't able to be
            // modified just because we're in statement mode for some other use.

            if (!context.Scope.ContainsVariable(path.SourceVariable.Name, onlyCheckTopLayer: true))
            {
                throw context.CreateExecutionErrorFor<StatementMethods>(ExceptionCode.AttemptedToIndirectlyModifyVariableWithinUsingBlock, path.SourceVariable.Name);
            }

            var token = path.ObtainableToken;

            var actualNewValue = newValue switch
            {
                DereferencedPath pathValue when pathValue.MissingPaths.Length == 0 => pathValue.ObtainableToken,
                DereferencedPath pathValue => throw context.CreateExecutionErrorFor<StatementMethods>(ExceptionCode.AttemptedToDereferenceMissingPath, pathValue.MissingPaths.Join('.'), pathValue.ObtainableToken.PropertyName),
                string pathValue => context.ResolveQueryPathIfPresent(pathValue) is IJsonToken pathToken ? pathToken : context.CreateTokenFrom(pathValue),
                object obj => context.CreateTokenFrom(obj),
                null => context.CreateTokenFrom(null)
            };

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
                string path => context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.Scope.AvailableClosures, path, JsonQueryMode.StartFromRoot),
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
                    var currentEvaluationToken = new EvaluationToken(context.Token.PropertyName, context.Token.ResolvedPropertyName, token, statement, default, true, isWithinStatementBlock: true);

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
