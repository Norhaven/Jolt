using Jolt.LanguageServer.Mapping;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Xunit;

namespace Jolt.LanguageServer.Tests
{
    public sealed class JsonMappingTests
    {
        [Fact]
        public void Strings_MapEachDecodedCharacterToItsOffset()
        {
            const string text = "{ \"a\\\"b\": \"x\\u0041y\" }";

            var root = (JsonObjectSyntax)JsonSyntaxParser.Parse(text);
            var key = root.Properties[0].Key;
            var value = (JsonStringSyntax)root.Properties[0].Value;

            Assert.Equal("a\"b", key.Value);
            Assert.Equal(new[] { 3, 4, 6, 7 }, key.RawOffsets);
            Assert.Equal("xAy", value.Value);
            Assert.Equal('y', text[value.RawOffsets[2]]);
            Assert.Equal('"', text[value.RawOffsets[3]]);
        }

        [Theory]
        [InlineData("{ \"a\": 1, }", 10)]
        [InlineData("{ \"a\" 1 }", 6)]
        [InlineData("[1, 2", 5)]
        [InlineData("{ \"a\": \"unterminated }", 7)]
        [InlineData("{ // comment\n }", 2)]
        public void InvalidJson_IsReportedAtItsOffset(string text, int offset)
        {
            var ex = Assert.Throws<JsonSyntaxException>(() => JsonSyntaxParser.Parse(text));

            Assert.Equal(offset, ex.Offset);
        }

        [Fact]
        public void PathIndex_FindsEveryPathThatSystemTextJsonProduces()
        {
            // Jolt reports paths from System.Text.Json, so every one of them must be found, including names that need quoting.
            const string text = @"{
                ""plain"": ""@x"",
                ""#foreach(@x in $.items) into 'result'"": [ { ""a.b"": ""#valueOf($['c d'])"" }, [ ""nested"" ] ],
                ""it's"": { ""[0]"": 1, ""$"": true, ""with space"": null },
                ""unicode é"": ""value""
            }";

            var index = new JsonPathIndex(JsonSyntaxParser.Parse(text));
            var paths = GetPaths(JsonNode.Parse(text)!).ToList();

            Assert.Equal(11, paths.Count);
            Assert.All(paths, x => Assert.NotNull(index.Find(x)));
        }

        private static IEnumerable<string> GetPaths(JsonNode node)
        {
            yield return node.GetPath();

            var children = node switch
            {
                JsonObject obj => obj.Select(x => x.Value),
                JsonArray array => array,
                _ => Enumerable.Empty<JsonNode?>()
            };

            foreach (var child in children.Where(x => x != null))
            {
                foreach (var path in GetPaths(child!))
                {
                    yield return path;
                }
            }
        }
    }
}
