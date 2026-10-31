using Jolt.Exceptions;
using Jolt.Library;
using Jolt.Structure;
using Jolt.Testing.Assertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Jolt.Testing.Unit
{
    public abstract class DelegateMethodTests
    {
        public static IEnumerable<object[]> GetTestTypes()
        {
            yield return new object[] { TestType.DotNet };
            yield return new object[] { TestType.Newtonsoft };
        }

        protected abstract IJsonContext CreateContext(TestType testType);

        private static long CountArguments(params object[] arguments) => arguments.Length;

        private IJsonToken Transform(TestType testType, string transformer, string source, params MethodRegistration[] methods)
        {
            var context = CreateContext(testType).UseTransformer(transformer).RegisterAllMethods(methods);
            var output = new JoltTransformer<IJsonContext>(context).Transform(source);

            return context.JsonTokenReader.Read(output).AsObject()["result"];
        }

        [Theory]
        [MemberData(nameof(GetTestTypes))]
        public void Transform_WithDelegateMethod_ShouldInvokeDelegate(TestType testType)
        {
            var greet = MethodRegistration.FromDelegate("greet", new Func<string, string>(name => $"Hello, {name}"));

            var result = Transform(testType, "{ \"result\": \"#greet(#valueOf($.name))\" }", "{ \"name\": \"Jolt\" }", greet);

            result.AsValue().ToTypeOf<string>().Should().Be("Hello, Jolt", "because the delegate should be invoked with the argument");
        }

        [Theory]
        [MemberData(nameof(GetTestTypes))]
        public void Transform_WithClosureDelegateMethod_ShouldUseCapturedState(TestType testType)
        {
            var calls = 0L;
            var next = MethodRegistration.FromDelegate("next", new Func<long>(() => ++calls));

            var result = Transform(testType, "{ \"result\": \"#next()\" }", "{}", next);

            result.AsValue().ToTypeOf<long>().Should().Be(1L, "because the delegate's captured state should be used");
            calls.Should().Be(1L, "because the delegate should be invoked once");
        }

        [Theory]
        [MemberData(nameof(GetTestTypes))]
        public void Transform_WithDelegateParamsArray_ShouldAcceptAnyNumberOfArguments(TestType testType)
        {
            var count = MethodRegistration.FromDelegate("count", new Func<object[], long>(CountArguments));

            var none = Transform(testType, "{ \"result\": \"#count()\" }", "{}", count);
            var several = Transform(testType, "{ \"result\": \"#count(1, 'two', #valueOf($.three))\" }", "{ \"three\": 3 }", count);

            none.AsValue().ToTypeOf<long>().Should().Be(0L, "because no arguments were provided");
            several.AsValue().ToTypeOf<long>().Should().Be(3L, "because three arguments were provided");
        }

        [Theory]
        [MemberData(nameof(GetTestTypes))]
        public void Transform_WithLambdaForDelegateParamsArray_ShouldAcceptLambda(TestType testType)
        {
            var count = MethodRegistration.FromDelegate("count", new Func<object[], long>(CountArguments));

            var result = Transform(testType, "{ \"result\": \"#count(#valueOf($.items), @x: @x)\" }", "{ \"items\": [1, 2] }", count);

            result.AsValue().ToTypeOf<long>().Should().Be(2L, "because an untyped parameter should accept a lambda as an argument");
        }

        [Theory]
        [MemberData(nameof(GetTestTypes))]
        public void Transform_WithLambdaForDelegateFuncParameter_ShouldInvokeLambda(TestType testType)
        {
            var sum = MethodRegistration.FromDelegate("sum", new Func<IEnumerable<long>, Func<long, long>, long>((values, selector) => values.Select(selector).Sum()));

            var result = Transform(testType, "{ \"result\": \"#sum(#valueOf($.numbers), @x: @x * 2)\" }", "{ \"numbers\": [1, 2, 3] }", sum);

            result.AsValue().ToTypeOf<long>().Should().Be(12L, "because the lambda should be invoked for each value");
        }

        [Theory]
        [MemberData(nameof(GetTestTypes))]
        public void Transform_WithLambdaForTypedDelegateParameter_ShouldThrow(TestType testType)
        {
            var greet = MethodRegistration.FromDelegate("greet", new Func<string, string>(name => $"Hello, {name}"));

            var exception = Assert.Throws<JoltExecutionException>(() => Transform(testType, "{ \"result\": \"#greet(@x: @x)\" }", "{}", greet));

            exception.Code.Should().Be(ExceptionCode.UnableToUseLambdaAsArgumentForNonDelegateParameter, "because a lambda can't be used for a typed parameter");
        }

        [Theory]
        [MemberData(nameof(GetTestTypes))]
        public void Validate_WithWrongArgumentCountForDelegateMethod_ShouldReportIssue(TestType testType)
        {
            var greet = MethodRegistration.FromDelegate("greet", new Func<string, string>(name => $"Hello, {name}"));
            var context = CreateContext(testType).UseTransformer("{ \"result\": \"#greet('a', 'b')\" }").RegisterMethod(greet);

            var issues = new JoltTransformer<IJsonContext>(context).Validate().ToArray();

            issues.Length.Should().Be(1, "because the method was called with too many arguments");
            issues[0].Code.Should().Be(ExceptionCode.MethodCallActualParameterCountExceedsFormalParameterCount, "because the method takes one argument");
        }

        [Fact]
        public void FromDelegate_WithoutName_ShouldThrow()
        {
            // A delegate method needs a name to be called by.
            Assert.Throws<ArgumentException>(() => MethodRegistration.FromDelegate(" ", new Func<long>(() => 1)));
        }
    }
}
