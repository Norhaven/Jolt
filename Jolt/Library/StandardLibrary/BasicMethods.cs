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
    internal sealed class BasicMethods
    {
        [JoltLibraryMethod("valueOf")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName | LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? ValueOf(string path, EvaluationContext context)
        {
            // A path written directly has already been checked by the parser, but it's only text at this point (e.g. "#valueOf('$.a')").

            if (context.JsonContext.QueryPathProvider.IsQueryPath(path) && !context.JsonContext.QueryPathProvider.IsValidQueryPath(path))
            {
                throw JsonPathText.CreateParsingErrorFor<BasicMethods>(context.JsonContext, path);
            }

            // ValueOf will always start a search from the root, other similar methods may search differently.

            return context.JsonContext.QueryPathProvider.SelectNodeAtPath(context.Scope.AvailableClosures, path, JsonQueryMode.StartFromRoot);
        }

        [JoltLibraryMethod("eval", isUnsafe: true)]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName | LibraryMethodTarget.PropertyValue)]
        public static EvaluationResult? Evaluate(string pathOrLiteral, EvaluationContext context)
        {
            var actualTokens = context.JsonContext.TokenReader.ReadToEnd(pathOrLiteral, context.Mode);

            if (!context.JsonContext.ExpressionParser.TryParseExpression(actualTokens, context.JsonContext, out var expression))
            {
                throw context.CreateExecutionErrorFor<BasicMethods>(ExceptionCode.UnableToParseEvalLibraryCallExpression, pathOrLiteral);
            }

            // The text wasn't known when the statement it's within was checked, so it needs to be held to the same rules here.

            if (context.Token.IsWithinStatementBlock)
            {
                foreach (var statement in StatementRules.FindStatementsWithin(expression))
                {
                    throw context.CreateExecutionErrorFor<BasicMethods>(ExceptionCode.UnableToUseMethodWithinNonRootStatementBlock, statement.Signature.Alias);
                }
            }

            var evaluationContext = new EvaluationContext(
                context.Mode,
                expression,
                context.JsonContext,
                context.Token,
                context.Scope,
                context.Transform);

            return context.JsonContext.ExpressionEvaluator.Evaluate(evaluationContext);
        }

        [JoltLibraryMethod("nameOf")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyName)]
        public static IJsonToken? NameOf(RangeVariable variable, EvaluationContext context)
        {
            return context.CreateTokenFrom(variable.Value?.PropertyName);
        }
    }
}
