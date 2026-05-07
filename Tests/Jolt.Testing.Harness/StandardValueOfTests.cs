using FluentAssertions;
using Jolt.Exceptions;
using Jolt.Json.Tests.Resources.TestAttributes;
using Jolt.Structure;
using Jolt.Testing.Small;
using Jolt.Testing.Small.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Testing.Harness
{
    public abstract class StandardValueOfTests
    {
        private static class Source
        {
            public const string ComplexObject = @"{ ""value"": ""test"" }";
        }

        private static class ValueTransformer
        {
            public const string Default = "#valueOf($.value)";
            public const string NoParameter = "#valueOf()";
            public const string NonPathParameter = "#valueOf(test)";
            public const string InvalidPathParameter = "#valueOf($.nowhere)";
            public const string TooManyParameters = "#valueOf($.value, $.wrong)";
        }

        [SourceHasString("test")]
        [TransformerIs(ValueTransformer.Default)]
        [ExpectsResult("test")]
        public void ValueOf_IsSuccessful_WithStringLiteral(string testName, SmallTest test) { Execute(test); }

        [SourceHasInteger(1)]
        [TransformerIs(ValueTransformer.Default)]
        [ExpectsResult(1)]
        public void ValueOf_IsSuccessful_WithIntegerLiteral() { }

        [SourceHasBoolean(true)]
        [TransformerIs(ValueTransformer.Default)]
        [ExpectsResult(true)]
        public void ValueOf_IsSuccessful_WithBooleanTrueLiteral() { }

        [SourceHasBoolean(false)]
        [TransformerIs(ValueTransformer.Default)]
        [ExpectsResult(false)]
        public void ValueOf_IsSuccessful_WithBooleanFalseLiteral() { }

        [SourceHasNoValue]
        [TransformerIs(ValueTransformer.InvalidPathParameter)]
        [ExpectsResult(null)]
        public void ValueOf_DefaultsToNull_WithInvalidPathParameter() { }

        [SourceHasComplexObject(Source.ComplexObject)]
        [TransformerIs(ValueTransformer.Default)]
        [ExpectsResultOfJsonObject(Source.ComplexObject)]
        public void ValueOf_ReturnsJsonObject_WithComplexObjectSource() { }

        [SourceHasNoValue]
        [TransformerIs(ValueTransformer.NoParameter)]
        [ExpectsException(ExceptionCode.MissingRequiredMethodParameter)]
        public void ValueOf_ThrowsException_WithMismatchedParameterCount() { }

        [SourceHasNoValue]
        [TransformerIs(ValueTransformer.NonPathParameter)]
        [ExpectsException(ExceptionCode.ExpectedBooleanLiteralTokenButFoundUnknownToken)]
        public void ValueOf_ThrowsException_WithNonPathParameter() { }

        [SourceHasString("test")]
        [TransformerIs(ValueTransformer.TooManyParameters)]
        [ExpectsException(ExceptionCode.MethodCallActualParameterCountExceedsFormalParameterCount)]
        public void ValueOf_ThrowsException_WithTooManyParameters() { }

        protected void Execute(SmallTest test)
        {
            var context = test.TestContext.CreateJsonContext(test.TestType);

            var currentContext = context.UseTransformer(test.TransformerJson.ToString());

            var transformer = new JoltTransformer<IJsonContext>(currentContext);

            try
            {
                var result = transformer.Transform(test.SourceJson.ToString());

                var jsonResult = currentContext.JsonTokenReader.Read(result) as IJsonObject;
                var value = jsonResult[test.ExpectsResult.PropertyName];

                if (value is null && test.ExpectsResult.Value is null)
                {
                    return;
                }

                value.Should().NotBeNull($"because we are expecting a value '{test.ExpectsResult.Value}' instead");

                if (value.Type == JsonTokenType.Object)
                {
                    var expectedToken = currentContext.JsonTokenReader.Read(test.ExpectsResult.Value?.ToString());
                    value.Equals(expectedToken).Should().BeTrue("because the transformed JSON should match the expectation");
                }
                else
                {
                    value.ToTypeOf<object>().Should().Be(test.ExpectsResult.Value, "because the result was expected by the test");
                }
            }
            catch (JoltException ex)
            {
                if (test.ExpectsException is null)
                {
                    throw;
                }

                if (test.ExpectsException.ExceptionType is null)
                {
                    test.ExpectsException.Code.Should().Be(ex.Code, "because this exception code was expected");
                }
                else
                {
                    test.ExpectsException.ExceptionType.Should().Be(ex.GetType(), "because this exception was expected");
                }
            }
            catch (Exception ex)
            {
                if (test.ExpectsException is null)
                {
                    throw;
                }

                test.ExpectsException.ExceptionType.Should().Be(ex.GetType(), "because this exception was expected");
            }
        }
    }
}
