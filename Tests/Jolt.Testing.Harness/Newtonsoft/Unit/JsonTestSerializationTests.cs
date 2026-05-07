using FluentAssertions;
using Jolt.Testing.Json;
using Jolt.Testing.Json.Attributes;
using Jolt.Testing.Unit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Xunit.Sdk;

namespace Jolt.Testing.Harness.Newtonsoft.Unit
{
    public sealed class JsonTestSerializationTests : SerializationTests
    {
        private const TestType _currentTestType = TestType.Newtonsoft;

        [Theory]
        [InlineData("LibraryMethodsWithVariables")]
        [InlineData("IndexingWithVariables")]
        [InlineData("ExternalMethods")]
        [InlineData("OperatorsAndVariables")]
        public void JsonTestCase_WithJsonTestFile_ShouldBeSerializable(string fileName)
        {
            var messages = new TestMessageSink();
            var testContext = new TestContext();
            var context = testContext.CreateJsonContext(_currentTestType);

            var attribute = new JsonTestAttribute(fileName, typeof(TestContext), _currentTestType);
            var discovery = new JsonTestDiscoverer(messages);

            var tests = discovery.Discover(new DiscoveryOptions(), new TestMethod(), new AttributeInfo(attribute)).ToArray();

            tests.Length.Should().BeGreaterThan(0, "because the test file should contain at least one test case");

            Trace.WriteLine($"[TEST] Found '{tests.Length}' tests in test file '{fileName}'");

            foreach (var test in tests.OfType<JoltTestCase<EndToEndTest>>())
            {
                var deserializedTest = SerializeAndDeserialize(test, messages);

                deserializedTest.Should().NotBeNull();
                deserializedTest.Data.Should().NotBeNull();
                deserializedTest.Data.Source.ToTypeOf<string>().Should().Be(test.Data.Source.ToTypeOf<string>());
                deserializedTest.Data.Transformer.ToTypeOf<string>().Should().Be(test.Data.Transformer.ToTypeOf<string>());
            }
        }

        [Fact]
        public void JsonTest_WithResult_ShouldBeSerializable()
        {
            var messages = new TestMessageSink();
            var context = new TestContext();
            var test = new EndToEndTest(
                messages,
                context,
                _currentTestType,
                "TestGroup",
                "TestName",
                0)
            {
                Source = context.ParseAsJsonObject(_currentTestType, "{\"key\": \"value\"}"),
                Transformer = context.ParseAsJsonObject(_currentTestType, "{\"transformerKey\": \"transformerValue\"}"),
                Result = context.ParseAsJsonObject(_currentTestType, "{\"resultKey\": \"resultValue\"}"),
            };

            var deserializedTest = SerializeAndDeserialize(test, messages);

            deserializedTest.Should().NotBeNull();
            deserializedTest.Source.ToTypeOf<string>().Should().Be(test.Source.ToTypeOf<string>());
            deserializedTest.Transformer.ToTypeOf<string>().Should().Be(test.Transformer.ToTypeOf<string>());
            deserializedTest.Result.Should().NotBeNull();
            deserializedTest.Result.ToTypeOf<string>().Should().Be(test.Result.ToTypeOf<string>());
        }

        [Fact]
        public void JsonTest_WithoutOptionalProperties_ShouldBeSerializable()
        {
            var messages = new TestMessageSink();
            var context = new TestContext();
            var test = new EndToEndTest(
                messages,
                context,
                _currentTestType,
                "TestGroup",
                "TestName",
                0)
            {
                Source = context.ParseAsJsonObject(_currentTestType, "{\"key\": \"value\"}"),
                Transformer = context.ParseAsJsonObject(_currentTestType, "{\"transformerKey\": \"transformerValue\"}"),
            };

            var deserializedTest = SerializeAndDeserialize(test, messages);

            deserializedTest.Should().NotBeNull();
            deserializedTest.Source.ToTypeOf<string>().Should().Be(test.Source.ToTypeOf<string>());
            deserializedTest.Transformer.ToTypeOf<string>().Should().Be(test.Transformer.ToTypeOf<string>());
            deserializedTest.PossibleExceptions.Should().BeNull();
            deserializedTest.PossibleExternalMethodSources.Should().BeNull();
        }

        [Fact]
        public void BasicJsonTest_ShouldBeSerializable()
        {
            var messages = new TestMessageSink();
            var context = new TestContext();

            var test = new EndToEndTest(
                messages,
                context,
                _currentTestType,
                "TestGroup",
                "TestName",
                0)
            {
                Source = context.ParseAsJsonObject(_currentTestType, "{\"key\": \"value\"}"),
                Transformer = context.ParseAsJsonObject(_currentTestType, "{\"transformerKey\": \"transformerValue\"}"),
                PossibleExceptions = new Dictionary<string, string>
                {
                    { "ExceptionCode1", "ExceptionMessage1" },
                    { "ExceptionCode2", "ExceptionMessage2" }
                },
                PossibleExternalMethodSources = new Dictionary<string, string>
                {
                    { "ExternalMethodSource1", "ExternalMethodCode1" },
                    { "ExternalMethodSource2", "ExternalMethodCode2" }
                },
            };
            
            var deserializedTest = SerializeAndDeserialize(test, messages);

            deserializedTest.Should().NotBeNull();
            deserializedTest.Source.ToTypeOf<string>().Should().Be(test.Source.ToTypeOf<string>());
            deserializedTest.PossibleExceptions.Should().NotBeNull();
            deserializedTest.PossibleExceptions.Should().ContainKeys(test.PossibleExceptions.Keys);
            deserializedTest.PossibleExternalMethodSources.Should().NotBeNull();
            deserializedTest.PossibleExternalMethodSources.Should().ContainKeys(test.PossibleExternalMethodSources.Keys);
        }
    }
}
