using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Jolt.Exceptions
{
    internal static class Error
    {
        private static readonly IDictionary<ExceptionCode, string> _errorsByCode = new Dictionary<ExceptionCode, string>
        { 
            // Parsing

            [ExceptionCode.MissingRequiredMethodParameter] = "Method '{0}' is missing a required parameter '{1}' of type '{2}'",
            [ExceptionCode.ExpectedTokenButFoundEndOfExpression] = "Unable to continue parsing, expected '{0}' but found end of expression",
            [ExceptionCode.ExpectedTokenButFoundDifferentToken] = "Unable to continue parsing, expected '{0}' but found '{1}' instead",
            [ExceptionCode.UnableToParseParenthesizedExpressionAtPosition] = "Unable to parse parenthesized expression at position '{0}'",
            [ExceptionCode.UnableToCloseParenthesizedExpressionAtPosition] = "Unable to close parenthesized expression at position '{0}'",
            [ExceptionCode.UnableToParseExpressionStartingWithDescription] = "Unable to parse expression starting with '{0}'",
            [ExceptionCode.UnableToUseMultipleRangeOperatorsWithinSameExpression] = "Unable to use multiple range operators within the same range expression",
            [ExceptionCode.UnableToParseRangeExpressionFormatIsInvalid] = "Unable to parse range index expression '{0}', format is invalid",
            [ExceptionCode.UnableToFindMethodImplementation] = "Unable to find method implementation for unknown method '{0}'",
            [ExceptionCode.UnableToCompleteParsingOfPipedMethodCall] = "Expected piped method call but could not complete parsing it",
            [ExceptionCode.ExpectedStartOfMethodCallButFoundDifferentTokenAtPosition] = "Expected a '#' character to begin a piped method but found character '{0}' at position '{1}'",
            [ExceptionCode.ExpectedStringLiteralPropertyNameButFoundDifferentToken] = "The property name must resolve to a string literal surrounded with single quote marks, but found token '{0}' instead",
            [ExceptionCode.UnableToCategorizeTokenAtPosition] = "Unable to categorize token '{0}' at position '{1}'.",
            [ExceptionCode.UnableToLocateExpectedCharactersInExpression] = "Unable to locate expected character(s) in expression",
            [ExceptionCode.UnableToLocateSpecificExpectedCharactersInExpression] = "Unable to locate expected character(s) '{0}' in expression",
            [ExceptionCode.ExpectedBooleanLiteralTokenButFoundUnknownToken] = "Expected a boolean value but found unsupported value '{0}' in expression",
            [ExceptionCode.UnableToParseLambdaExpressionBodyAtPosition] = "Expected a lambda expression body at position '{0}' but found '{1}'",

            // Resolution

            [ExceptionCode.UnableToLocateInstanceMethod] = "Unable to locate instance method '{0}' during method resolution, no method context was provided",
            [ExceptionCode.UnableToLocateTypeForStaticMethod] = "Unable to locate defining type '{0}' for method '{1}' during method resolution",
            [ExceptionCode.UnableToLocateStaticMethodWithProvidedType] = "Unable to locate static method '{0}' during method resolution within type '{1}'",

            // Execution

            [ExceptionCode.UnableToApplyChangesToUnsupportedParentToken] = "Unable to apply changes to parent token with unsupported type '{0}'",
            [ExceptionCode.EncounteredMultipleNonSystemMethodsWithSameNameOrAlias] = "Encountered multiple non-system methods with the name or alias '{0}'",
            [ExceptionCode.UnableToEvaluateExpressionWithOperatorAndArguments] = "Unable to evaluate expression '{0} {1} {2}'",
            [ExceptionCode.UnableToEvaluateExpressionWithNullArgument] = "Unable to evaluate expression with operator '{0}' using a null value",
            [ExceptionCode.UnableToEvaluateExpressionWithBooleanArgument] = "Unable to perform math using operator '{0}' on a boolean value",
            [ExceptionCode.UnableToEvaluateExpressionWithOnlyStrings] = "Unable to perform math on strings, found unexpected operator '{0}' in an expression containing only strings",
            [ExceptionCode.UnableToEvaluateExpressionWithMismatchedTypes] = "Unable to perform math with mismatched types in expression '{0} {1} {2}'",
            [ExceptionCode.UnableToConvertValueToInteger] = "Unable to convert value '{0}' to an integer",
            [ExceptionCode.UnableToConvertValueToDecimal] = "Unable to convert value '{0}' to a decimal",
            [ExceptionCode.UnableToConvertValueToBoolean] = "Unable to convert value '{0}' to a boolean",
            [ExceptionCode.UnableToConvertToBestTypeForLiteralValue] = "Unable to convert to best type for literal value '{0}'",
            [ExceptionCode.UnableToUseMethodWithinPropertyName] = "Unable to use method '{0}' within a property name",
            [ExceptionCode.UnableToUseMethodWithinPropertyValue] = "Unable to use method '{0}' within a property value",
            [ExceptionCode.UnableToApplyMethodResultsWithUnknownEvaluationMode] = "Unable to apply results of method invocation for unknown evaluation mode '{0}'",
            [ExceptionCode.UnableToInvokeInstanceMethodWithoutMethodContext] = "Unable to execute external instance method '{0}', no method context was provided",
            [ExceptionCode.UnableToLocateInstanceMethodWithProvidedMethodContext] = "Unable to locate instance method '{0}' on type '{1}'",
            [ExceptionCode.UnableToInvokeMethodWithUnknownCallType] = "Unable to invoke method with unknown call type '{0}'",

            [ExceptionCode.UnableToCompleteIncludeIfLibraryCallDueToNonBooleanCondition] = "Unable to complete #includeIf() call, failed to identify condition value '{0}' as a boolean value",
            [ExceptionCode.UnableToParseEvalLibraryCallExpression] = "Unable to parse expression '{0}' within #eval call as a path or literal value",
            [ExceptionCode.UnableToPerformLoopLibraryCallOnNonLoopableToken] = "Unable to loop using non-enumerable token of type '{token.Type}'",
            [ExceptionCode.UnableToPerformLoopLibraryCallOnNonLoopableSourceToken] = "Unable to loop over non-enumerable source token of type '{0}'",
            [ExceptionCode.UnableToPerformLoopLibraryCallDueToMissingContentTemplate] = "Unable to locate loop content template for unsupported token type '{0}'",
            [ExceptionCode.MethodCallActualParameterCountExceedsFormalParameterCount] = "The number of parameters passed to method '{0}' exceed the allowed amount of '{1}'",
            [ExceptionCode.ReferencedRangeVariableWithNoValue] = "Referenced range variable '{0}' which had a null value",
            [ExceptionCode.UnableToPerformLoopLibraryCallDueToInvalidParameter] = "Unable to loop on value '{0}' which is not a path or a variable reference",
            [ExceptionCode.ExpectedLambdaResultToBeBooleanButFoundDifferentToken] = "Expected lambda result to be a boolean value but found '{0}' instead",
            [ExceptionCode.UnableToParsePropertyDereferenceChain] = "Unable to parse property dereference chain, found value '{0}'",
            [ExceptionCode.EncounteredValueInDereferenceChainButExpectedObject] = "Encountered value in dereference chain for property '{0}' but expected object",
            [ExceptionCode.EncounteredNonObjectInDereferenceChainButExpectedObject] = "Encountered non-object in dereference chain for property '{0}' but expected object"

        };

        public static JoltException CreateParsingErrorFrom(ExceptionCode code, params object[] parameters) => BuildExceptionFrom(code, parameters, x => new JoltParsingException(code, x));
        public static JoltException CreateExecutionErrorFrom(ExceptionCode code, params object[] parameters) => BuildExceptionFrom(code, parameters, x => new JoltExecutionException(code, x));
        public static JoltException CreateResolutionErrorFrom(ExceptionCode code, string typeName, string methodName, params object[] parameters) => BuildExceptionFrom(code, parameters, x => new JoltMethodResolutionException(code, typeName, methodName, x));

        private static JoltException BuildExceptionFrom(ExceptionCode code, object[] parameters, Func<string, JoltException> createWith)
        {
            if (!_errorsByCode.TryGetValue(code, out var messageContent))
            {
                throw new ArgumentOutOfRangeException($"Unable to create error from unsupported exception code '{code}'");
            }

            var message = string.Format(messageContent, parameters);

            return createWith(message);
        }
    }
}
