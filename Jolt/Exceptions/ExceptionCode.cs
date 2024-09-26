using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Jolt.Exceptions
{
    public enum ExceptionCode
    {
        // Parsing

        [Description("JLT100")]
        MissingRequiredMethodParameter,
        [Description("JLT101")]
        ExpectedTokenButFoundEndOfExpression,
        [Description("JLT102")]
        ExpectedTokenButFoundDifferentToken,
        [Description("JLT103")]
        UnableToParseParenthesizedExpressionAtPosition,
        [Description("JLT104")]
        UnableToCloseParenthesizedExpressionAtPosition,
        [Description("JLT105")]
        UnableToParseExpressionStartingWithDescription,
        [Description("JLT106")]
        UnableToUseMultipleRangeOperatorsWithinSameExpression,
        [Description("JLT107")]
        UnableToParseRangeExpressionFormatIsInvalid,
        [Description("JLT108")]
        UnableToFindMethodImplementation,
        [Description("JLT109")]
        UnableToCompleteParsingOfPipedMethodCall,
        [Description("JLT110")]
        ExpectedStartOfMethodCallButFoundDifferentTokenAtPosition,
        [Description("JLT111")]
        ExpectedStringLiteralPropertyNameButFoundDifferentToken,
        [Description("JLT112")]
        UnableToCategorizeTokenAtPosition,
        [Description("JLT113")]
        UnableToLocateExpectedCharactersInExpression,
        [Description("JLT114")]
        UnableToLocateSpecificExpectedCharactersInExpression,
        [Description("JLT115")]
        ExpectedBooleanLiteralTokenButFoundUnknownToken,
        [Description("JLT116")]
        UnableToParseLambdaExpressionBodyAtPosition,
        [Description("JLT117")]
        UnableToParsePropertyDereferenceChain,
        [Description("JLT118")]
        ExpectedInKeywordButFoundUnexpectedToken,
        [Description("JLT119")]
        UnableToParseEnumerationSourceForVariable,
        [Description("JLT120")]
        ExpectedRangeVariableAfterSemicolonButFoundTokenInstead,
        [Description("JLT121")]
        ExpectedAsKeywordButFoundUnexpectedToken,
        [Description("JLT122")]
        UnableToParseVariableAlias,

        // Resolution

        [Description("JLT300")]
        UnableToLocateTypeForStaticMethod,
        [Description("JLT301")]
        UnableToLocateStaticMethodWithProvidedType,

        // Execution

        [Description("JLT501")]
        UnableToLocateInstanceMethod,
        [Description("JLT502")]
        UnableToApplyChangesToUnsupportedParentToken,
        [Description("JLT503")]
        EncounteredMultipleNonSystemMethodsWithSameNameOrAlias,
        [Description("JLT504")]
        UnableToEvaluateExpressionWithOperatorAndArguments,
        [Description("JLT505")]
        UnableToEvaluateExpressionWithNullArgument,
        [Description("JLT506")]
        UnableToEvaluateExpressionWithBooleanArgument,
        [Description("JLT507")]
        UnableToEvaluateExpressionWithOnlyStrings,
        [Description("JLT508")]
        UnableToEvaluateExpressionWithMismatchedTypes,
        [Description("JLT509")]
        UnableToConvertValueToInteger,
        [Description("JLT510")]
        UnableToConvertValueToDecimal,
        [Description("JLT511")]
        UnableToConvertValueToBoolean,
        [Description("JLT512")]
        UnableToConvertToBestTypeForLiteralValue,
        [Description("JLT513")]
        UnableToUseMethodWithinPropertyName,
        [Description("JLT514")]
        UnableToUseMethodWithinPropertyValue,
        [Description("JLT515")]
        UnableToApplyMethodResultsWithUnknownEvaluationMode,
        [Description("JLT516")]
        UnableToInvokeInstanceMethodWithoutMethodContext,
        [Description("JLT517")]
        UnableToLocateInstanceMethodWithProvidedMethodContext,
        [Description("JLT518")]
        UnableToInvokeMethodWithUnknownCallType,
        [Description("JLT519")]
        UnableToCompleteIncludeIfLibraryCallDueToNonBooleanCondition,
        [Description("JLT520")]
        UnableToParseEvalLibraryCallExpression,
        [Description("JLT521")]
        UnableToPerformLoopLibraryCallOnNonLoopableToken,
        [Description("JLT522")]
        UnableToPerformLoopLibraryCallOnNonLoopableSourceToken,
        [Description("JLT523")]
        UnableToPerformLoopLibraryCallDueToMissingContentTemplate,
        [Description("JLT524")]
        MethodCallActualParameterCountExceedsFormalParameterCount,
        //[Description("JLT525")]
        //ReferencedRangeVariableWithNoValue,
        [Description("JLT526")]
        UnableToPerformLoopLibraryCallDueToInvalidParameter,
        [Description("JLT527")]
        ExpectedLambdaResultToBeBooleanButFoundDifferentToken,
        [Description("JLT528")]
        EncounteredValueInDereferenceChainButExpectedObject,
        [Description("JLT529")]
        EncounteredNonObjectInDereferenceChainButExpectedObject,
        [Description("JLT530")]
        AttemptedToDereferenceMissingPath,
        [Description("JLT531")]
        UnableToRemoveNodeFromNonObjectParent,
        [Description("JLT532")]
        UnableToSetPropertyOnNonObjectReference,
        [Description("JLT533")]
        UnableToPerformUsingLibraryCallOnNonArrayToken,
        [Description("JLT534")]
        UnableToPerformUsingLibraryCallDueToInvalidParameter,
        [Description("JLT535")]
        UnableToPerformUsingLibraryCallOnNonObjectToken,
        [Description("JLT536")]
        UnableToUseMethodWithinStatementBlock,
        [Description("JLT537")]
        UnableToUseMethodOutsideOfStatementBlock,
        [Description("JLT538")]
        UnableToUseMethodWithinNonRootStatementBlock,
        [Description("JLT539")]
        AttemptedToIndirectlyModifyVariableWithinUsingBlock,
        [Description("JLT540")]
        UnableToResolveNewPropertyNameForUnsupportedResultOfType
    }
}
