using Jolt.LanguageServer.Protocol;
using Jolt.LanguageServer.Validation;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Jolt.LanguageServer.Tests
{
    public sealed class DocumentValidatorTests
    {
        private static readonly DocumentContext NoExternals = new DocumentContext(new string[0], new string[0]);

        private static List<Diagnostic> Validate(string text, DocumentContext? context = null, ValidationSettings? settings = null) =>
            DocumentValidator.Validate(text, context ?? NoExternals, settings ?? ValidationSettings.Default);

        /// <summary>
        /// Gets the text that a diagnostic covers, for documents written on a single line.
        /// </summary>
        private static string Covered(string text, Diagnostic diagnostic)
        {
            Assert.Equal(0, diagnostic.Range.Start.Line);
            Assert.Equal(0, diagnostic.Range.End.Line);

            return text.Substring(diagnostic.Range.Start.Character, diagnostic.Range.End.Character - diagnostic.Range.Start.Character);
        }

        [Fact]
        public void ValidTransformer_HasNoDiagnostics()
        {
            Assert.Empty(Validate("{ \"@x\": \"#valueOf($.a)\", \"result\": \"@x->#select(@item: @item.name)\", \"total\": \"#reduce($.values, @acc;@current: @acc + @current)\" }"));
        }

        [Fact]
        public void UndeclaredVariable_CoversTheVariable()
        {
            const string text = "{ \"result\": \"#valueOf($.a) + @missing\" }";

            var diagnostic = Assert.Single(Validate(text));

            Assert.Equal("@missing", Covered(text, diagnostic));
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Equal("JLT562", diagnostic.Code);
            Assert.Equal("Jolt", diagnostic.Source);
        }

        [Fact]
        public void RepeatedIssues_CoverSuccessiveOccurrences()
        {
            const string text = "{ \"result\": \"@missing + @missing\" }";

            var diagnostics = Validate(text);

            Assert.Equal(2, diagnostics.Count);
            Assert.Equal(text.IndexOf("@missing"), diagnostics[0].Range.Start.Character);
            Assert.Equal(text.LastIndexOf("@missing"), diagnostics[1].Range.Start.Character);
        }

        [Fact]
        public void VariableName_IsNotMatchedWithinALongerName()
        {
            const string text = "{ \"@xs\": \"1\", \"result\": \"@xs + @x\" }";

            var diagnostic = Assert.Single(Validate(text));

            Assert.Equal(text.IndexOf("@x\""), diagnostic.Range.Start.Character);
            Assert.Equal("@x", Covered(text, diagnostic));
        }

        [Fact]
        public void IssueInPropertyName_CoversThePropertyName()
        {
            const string text = "{ \"#foreach(@x in @missing) into 'items'\": [ { \"value\": \"@missing\" } ] }";

            var diagnostics = Validate(text);

            Assert.Equal(2, diagnostics.Count);
            Assert.Equal(text.IndexOf("@missing"), diagnostics[0].Range.Start.Character);
            Assert.Equal(text.LastIndexOf("@missing"), diagnostics[1].Range.Start.Character);
        }

        [Fact]
        public void MethodIssues_CoverTheMethodName()
        {
            const string text = "{ \"result\": \"#currentDateTime('extra')\" }";

            var diagnostic = Assert.Single(Validate(text));

            Assert.Equal("#currentDateTime", Covered(text, diagnostic));
            Assert.Equal("Method 'currentDateTime' expects no arguments but received 1.", diagnostic.Message);
        }

        [Fact]
        public void SyntaxError_AtTheEndOfTheExpression_CoversItsLastCharacter()
        {
            const string text = "{ \"result\": \"#valueOf(\" }";

            var diagnostic = Assert.Single(Validate(text));

            Assert.Equal("(", Covered(text, diagnostic));
            Assert.Equal(text.IndexOf('('), diagnostic.Range.Start.Character);
            Assert.StartsWith("Unable to continue parsing, expected ", diagnostic.Message);
            Assert.EndsWith(" but found end of expression", diagnostic.Message);
        }

        [Theory]
        [InlineData("#valueOf($.a))", ")", "JLT572")]
        [InlineData("#valueOf($.a) @b", "@b", "JLT572")]
        [InlineData("(#valueOf($.a) == )", "==", "JLT573")]
        public void IncompleteOrExtraContent_CoversTheProblem(string expression, string expectedText, string expectedCode)
        {
            var text = $"{{ \"result\": \"{expression}\" }}";

            var diagnostic = Assert.Single(Validate(text));

            Assert.Equal(expectedText, Covered(text, diagnostic));
            Assert.Equal(expectedCode, diagnostic.Code);
        }

        [Fact]
        public void UnknownMethod_CoversTheMethodName()
        {
            const string text = "{ \"result\": \"#valueOf($.a)->#reverseString()\" }";

            var diagnostic = Assert.Single(Validate(text));

            Assert.Equal("#reverseString", Covered(text, diagnostic));
        }

        [Fact]
        public void Spans_AreMappedThroughEscapeSequences()
        {
            // "\u0041" is six characters in the document but one in the expression, so the unknown method after it moves.
            const string text = "{ \"result\": \"#valueOf('\\u0041')->#nope()\" }";

            var diagnostic = Assert.Single(Validate(text));

            Assert.Equal("#nope", Covered(text, diagnostic));
        }

        [Fact]
        public void EscapedCharacters_AreAccountedForInRanges()
        {
            // "A" is six characters in the document but one in the expression, which shifts everything after it.
            const string text = "{ \"result\": \"#valueOf('\\u0041b') + @missing\" }";

            var diagnostic = Assert.Single(Validate(text));

            Assert.Equal("@missing", Covered(text, diagnostic));
        }

        [Fact]
        public void MultipleLines_AreMappedToLinesAndCharacters()
        {
            const string text = "{\r\n  \"a\": \"1\",\n  \"result\": \"@missing\"\n}";

            var diagnostic = Assert.Single(Validate(text));

            Assert.Equal(2, diagnostic.Range.Start.Line);
            Assert.Equal(13, diagnostic.Range.Start.Character);
            Assert.Equal(21, diagnostic.Range.End.Character);
        }

        [Fact]
        public void InvalidJson_IsReportedWhereItIsInvalid()
        {
            var diagnostic = Assert.Single(Validate("{ \"a\": \"1\" \"b\": \"2\" }"));

            Assert.Equal(11, diagnostic.Range.Start.Character);
            Assert.StartsWith("Invalid JSON: Expected ',' or '}'", diagnostic.Message);
        }

        [Fact]
        public void DuplicatePropertyNames_AreReportedAtTheTransformer()
        {
            var diagnostic = Assert.Single(Validate("{ \"a\": \"1\", \"a\": \"2\" }"));

            Assert.Equal(0, diagnostic.Range.Start.Character);
            Assert.StartsWith("Unable to validate this transformer:", diagnostic.Message);
        }

        [Fact]
        public void UnknownMethods_AreReportedOnlyWhenTheExternalMethodsAreKnown()
        {
            const string text = "{ \"result\": \"#reverseString($.a)\" }";

            Assert.Single(Validate(text));
            Assert.Empty(Validate(text, DocumentContext.Unknown));
        }

        [Fact]
        public void ExternalMethods_AreResolvedAndTheirArgumentsValidated()
        {
            const string text = "{ \"result\": \"#reverseString($.a, 'b', @missing)\", \"name\": \"#reverseString($.a)\" }";

            var diagnostic = Assert.Single(Validate(text, new DocumentContext(new[] { "reverseString" }, new string[0])));

            Assert.Equal("@missing", Covered(text, diagnostic));
        }

        [Fact]
        public void ExternalMethods_AreNotValidInPropertyNames()
        {
            const string text = "{ \"#reverseString($.a)\": \"1\" }";

            var diagnostic = Assert.Single(Validate(text, new DocumentContext(new[] { "reverseString" }, new string[0])));

            Assert.Equal("#reverseString", Covered(text, diagnostic));
        }

        [Fact]
        public void MissingTransformers_AreReportedOnlyWhenTheTransformersAreKnown()
        {
            const string text = "{ \"result\": \"#transformWith($.a, 'address')\" }";

            var diagnostic = Assert.Single(Validate(text));

            Assert.Equal("'address'", Covered(text, diagnostic));
            Assert.Empty(Validate(text, new DocumentContext(new string[0], new[] { "address" })));
            Assert.Empty(Validate(text, DocumentContext.Unknown));
        }

        [Fact]
        public void UnsafeMethods_AreWarningsUnlessAllowed()
        {
            const string text = "{ \"result\": \"#eval('1 + 1')\" }";

            var diagnostic = Assert.Single(Validate(text));

            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.Empty(Validate(text, settings: new ValidationSettings(allowUnsafeMethods: true)));
        }

        [Fact]
        public void TestDocuments_ValidateOnlyTheTransformerInEachTest()
        {
            const string text = "{ \"testGroups\": [ { \"source\": { \"email\": \"@someone\" }, \"tests\": [ { \"transformer\": { \"a\": \"@x\" } }, { \"transformer\": { \"@x\": \"1\", \"b\": \"@x\" } } ] } ] }";

            var diagnostic = Assert.Single(Validate(text));

            Assert.Equal("@x", Covered(text, diagnostic));
            Assert.Equal(text.IndexOf("@x"), diagnostic.Range.Start.Character);
        }

        [Fact]
        public void NullPropertyValue_StillLocatesAnIssueInItsName()
        {
            const string text = "{ \"#foreach(@x in @missing) into 'items'\": null }";

            var diagnostics = Validate(text);

            Assert.Contains(diagnostics, x => Covered(text, x) == "@missing");
        }
    }
}
