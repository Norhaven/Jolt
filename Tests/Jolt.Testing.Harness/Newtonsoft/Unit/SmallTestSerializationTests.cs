using Jolt.Json.Tests.Resources.TestAttributes;
using Jolt.Testing.Assertions;
using Jolt.Testing.Small;
using Jolt.Testing.Unit;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Sdk;

namespace Jolt.Testing.Harness.Newtonsoft.Unit
{
    public sealed class SmallTestSerializationTests : SerializationTests
    {
        [Fact]
        public void SmallTest_ShouldBeSerializable()
        {
            var messages = new TestMessageSink();
            var test = new SmallTest(messages, new TestContext(), TestType.Newtonsoft, "TestGroup", "TestName", 0, @"{ ""value"": ""test"" }", @"#valueOf($.value)", new ExpectsResultAttribute("test"), null);

            var deserializedTest = SerializeAndDeserialize(test, messages);

            deserializedTest.Should().NotBeNull();
            deserializedTest.SourceJson.Should().Be(test.SourceJson);
            deserializedTest.TransformerJson.Should().Be(test.TransformerJson);
            deserializedTest.ExpectsResult.Should().NotBeNull();
            deserializedTest.ExpectsResult.Value.Should().Be(test.ExpectsResult.Value);
            deserializedTest.ExpectsException.Should().BeNull();
        }
    }
}
