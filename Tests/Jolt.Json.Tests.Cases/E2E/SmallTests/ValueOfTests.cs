using Jolt.Exceptions;
using Jolt.Json.Tests.Resources;
using Jolt.Json.Tests.Resources.TestAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.Cases.E2E.SmallTests
{
    public abstract class ValueOfTests : SmallTest
    {
        private static class Source
        {
            public const string ComplexObject = @"{ ""value"": ""test"" }";
        }

        private static class ValueTransformer
        {
            public const string Default = "#valueOf($." + SourceProperty.Value + ")";
            public const string NoParameter = "#valueOf()";
            public const string NonPathParameter = "#valueOf(test)";
            public const string InvalidPathParameter = "#valueOf($.nowhere)";
            public const string TooManyParameters = "#valueOf($.value, $.wrong)";
        }

        [SmallTestDefinition]
        [SourceHasString("test")]
        [TransformerIs(ValueTransformer.Default)]
        [ExpectsResult("test")]
        public void ValueOf_IsSuccessful_WithStringLiteral() { }

        [SmallTestDefinition]
        [SourceHasInteger(1)]
        [TransformerIs(ValueTransformer.Default)]
        [ExpectsResult(1)]
        public void ValueOf_IsSuccessful_WithIntegerLiteral() { }

        [SmallTestDefinition]
        [SourceHasBoolean(true)]
        [TransformerIs(ValueTransformer.Default)]
        [ExpectsResult(true)]
        public void ValueOf_IsSuccessful_WithBooleanTrueLiteral() { }

        [SmallTestDefinition]
        [SourceHasBoolean(false)]
        [TransformerIs(ValueTransformer.Default)]
        [ExpectsResult(false)]
        public void ValueOf_IsSuccessful_WithBooleanFalseLiteral() { }

        [SmallTestDefinition]
        [SourceHasNoValue]
        [TransformerIs(ValueTransformer.InvalidPathParameter)]
        [ExpectsResult(null)]
        public void ValueOf_DefaultsToNull_WithInvalidPathParameter() { }

        [SmallTestDefinition]
        [SourceHasComplexObject(Source.ComplexObject)]
        [TransformerIs(ValueTransformer.Default)]
        [ExpectsResultOfJsonObject(Source.ComplexObject)]
        public void ValueOf_ReturnsJsonObject_WithComplexObjectSource() { }

        [SmallTestDefinition]
        [SourceHasNoValue]
        [TransformerIs(ValueTransformer.NoParameter)]
        [ExpectsException(ExceptionCode.MissingRequiredMethodParameter)]
        public void ValueOf_ThrowsException_WithMismatchedParameterCount() { }

        [SmallTestDefinition]
        [SourceHasNoValue]
        [TransformerIs(ValueTransformer.NonPathParameter)]
        [ExpectsException(ExceptionCode.ExpectedBooleanLiteralTokenButFoundUnknownToken)]
        public void ValueOf_ThrowsException_WithNonPathParameter() { }

        [SmallTestDefinition]
        [SourceHasString("test")]
        [TransformerIs(ValueTransformer.TooManyParameters)]
        [ExpectsException(ExceptionCode.MethodCallActualParameterCountExceedsFormalParameterCount)]
        public void ValueOf_ThrowsException_WithTooManyParameters() { }
    }
}