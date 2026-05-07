using FluentAssertions;
using Jolt.Testing.Unit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Testing.Harness.DotNet.Unit
{
    public sealed class JsonObjectTests : IJsonObjectTests
    {
        protected override IJsonContext CreateContext() => new TestContext().CreateJsonContext(TestType.DotNet);

        [Fact]
        public void Dictionary_ShouldBeParsedAsJsonObject()
        {
            var context = CreateContext();
            var jsonObject = CreateObjectFromDictionary(new Dictionary<string, string> { ["key"] = "value" });

            jsonObject.Should().NotBeNull("because the JSON string represents a valid JSON object");
            jsonObject["key"].Should().NotBeNull("because the JSON object contains a key named 'key'");
            jsonObject["key"]!.ToTypeOf<string>().Should().Be("value", "because the value associated with 'key' should be 'value'");
        }

        [Fact]
        public void JsonObject_ShouldBeConvertibleToDictionary()
        {
            var context = CreateContext();
            var jsonObject = context.JsonTokenReader.Read(@"{ ""key"": ""value"" }")!.AsObject();
            var dictionary = CreateDictionaryFromObject(jsonObject);

            dictionary.Should().NotBeNull("because the JSON object can be converted to a dictionary");
            dictionary.Should().ContainKey("key").WhoseValue.Should().Be("value", "because the dictionary should contain the key 'key' with value 'value'");
        }
    }
}
