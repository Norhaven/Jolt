using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Jolt.Exceptions
{
    public enum ExceptionCode
    {
        [Description("JLT100")]
        MissingRequiredMethodParameter,
        ExpectedTokenButFoundEndOfExpression,
        ExpectedTokenButFoundDifferentToken,
        UnableToParseParenthesizedExpressionAtPosition,
        UnableToCloseParenthesizedExpressionAtPosition,
        UnableToParseExpressionStartingWithDescription,
        UnableToUseMultipleRangeOperatorsWithinSameExpression,
        UnableToParseRangeExpressionFormatIsInvalid,
        UnableToFindMethodImplementation,
        UnableToCompleteParsingOfPipedMethodCall,
        ExpectedStartOfMethodCallButFoundDifferentTokenAtPosition,
        ExpectedStringLiteralPropertyNameButFoundDifferentToken,
        UnableToCategorizeTokenAtPosition,
        UnableToLocateExpectedCharactersInExpression,
        UnableToLocateSpecificExpectedCharactersInExpression,
        UnableToLocateInstanceMethod,
        UnableToApplyChangesToUnsupportedParentToken,
        EncounteredMultipleNonSystemMethodsWithSameNameOrAlias,
        UnableToEvaluateExpressionWithOperatorAndArguments,
        UnableToEvaluateExpressionWithNullArgument,
        UnableToEvaluateExpressionWithBooleanArgument,
        UnableToEvaluateExpressionWithOnlyStrings,
        UnableToEvaluateExpressionWithMismatchedTypes,
        UnableToConvertValueToInteger,
        UnableToConvertValueToDecimal,
        UnableToConvertValueToBoolean,
        UnableToConvertToBestTypeForLiteralValue,
        UnableToUseMethodWithinPropertyName,
        UnableToUseMethodWithinPropertyValue,
        UnableToApplyMethodResultsWithUnknownEvaluationMode,
        UnableToInvokeInstanceMethodWithoutMethodContext,
        UnableToLocateInstanceMethodWithProvidedMethodContext,
        UnableToInvokeMethodWithUnknownCallType,
        UnableToCompleteIncludeIfLibraryCallDueToNonBooleanCondition,
        UnableToParseEvalLibraryCallExpression,
        UnableToPerformLoopLibraryCallOnNonLoopableToken,
        UnableToPerformLoopLibraryCallOnNonLoopableSourceToken,
        UnableToPerformLoopLibraryCallDueToMissingContentTemplate
    }
}
