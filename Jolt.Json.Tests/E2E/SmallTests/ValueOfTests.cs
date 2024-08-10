using Jolt.Exceptions;
using Jolt.Json.Tests.TestAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.E2E.SmallTests;

public abstract partial class ValueOfTests(IJsonContext context) : SmallTest(context)
{
    private static class Source
    {
        public const string ComplexObject = @"{ ""value"": ""test"" }";
    }

    private static class Transformer
    {
        public const string Default = $"#valueOf($.{SourceProperty.Value})";
        public const string NoParameter = "#valueOf()";
        public const string NonPathParameter = "#valueOf(test)";
        public const string InvalidPathParameter = "#valueOf($.nowhere)";
        public const string TooManyParameters = "#valueOf($.value, $.wrong)";
    }

    [Fact]
    [SourceHasString("test")]
    [TransformerIs(Transformer.Default)]
    [ExpectsResult("test")]
    public void ValueOf_IsSuccessful_WithStringLiteral() => ExecuteSmallTest();

    [Fact]
    [SourceHasInteger(1)]
    [TransformerIs(Transformer.Default)]
    [ExpectsResult(1)]
    public void ValueOf_IsSuccessful_WithIntegerLiteral() => ExecuteSmallTest();

    [Fact]
    [SourceHasBoolean(true)]
    [TransformerIs(Transformer.Default)]
    [ExpectsResult(true)]
    public void ValueOf_IsSuccessful_WithBooleanTrueLiteral() => ExecuteSmallTest();

    [Fact]
    [SourceHasBoolean(false)]
    [TransformerIs(Transformer.Default)]
    [ExpectsResult(false)]
    public void ValueOf_IsSuccessful_WithBooleanFalseLiteral() => ExecuteSmallTest();

    [Fact]
    [SourceHasNoValue]
    [TransformerIs(Transformer.InvalidPathParameter)]
    [ExpectsResult(null)]
    public void ValueOf_DefaultsToNull_WithInvalidPathParameter() => ExecuteSmallTest();

    [Fact]
    [SourceHasComplexObject(Source.ComplexObject)]
    [TransformerIs(Transformer.Default)]
    [ExpectsResultOfJsonObject(Source.ComplexObject)]
    public void ValueOf_ReturnsJsonObject_WithComplexObjectSource() => ExecuteSmallTest();

    [Fact]
    [SourceHasNoValue]
    [TransformerIs(Transformer.NoParameter)]
    [ExpectsException(ExceptionCode.MissingRequiredMethodParameter)]
    public void ValueOf_ThrowsException_WithMismatchedParameterCount() => ExecuteSmallTest();

    [Fact]
    [SourceHasNoValue]
    [TransformerIs(Transformer.NonPathParameter)]
    [ExpectsException(ExceptionCode.ExpectedBooleanLiteralTokenButFoundUnknownToken)]
    public void ValueOf_ThrowsException_WithNonPathParameter() => ExecuteSmallTest();

    [Fact]
    [SourceHasString("test")]
    [TransformerIs(Transformer.TooManyParameters)]
    [ExpectsException(ExceptionCode.MethodCallActualParameterCountExceedsFormalParameterCount)]
    public void ValueOf_ThrowsException_WithTooManyParameters() => ExecuteSmallTest();
}
