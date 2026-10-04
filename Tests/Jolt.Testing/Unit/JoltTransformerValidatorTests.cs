using Jolt.Testing.Assertions;
using Jolt.Exceptions;
using Jolt.Library;
using Jolt.Structure;
using Jolt.Testing.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Jolt.Testing.Unit
{
    public abstract class JoltTransformerValidatorTests
    {
        protected sealed class ValidationTest
        {
            public string Name { get; set; }
            public IJsonObject Transformer { get; set; }
            public string[] RegisteredTransformers { get; set; }
            public string[] ExpectedIssues { get; set; }
        }

        public static IEnumerable<object[]> GetTestData()
        {
            var validationsFile = "Validations";

            yield return new object[] { TestType.DotNet, validationsFile };
            yield return new object[] { TestType.Newtonsoft, validationsFile };
        }
        
        protected abstract IJsonContext CreateContext(TestType testType);

        private void ExecuteValidationTests(TestType testType, string validationsFile)
        {
            var context = CreateContext(testType);

            var validationsJson = TestResource.ReadValidations(validationsFile);
            var validations = context.JsonTokenReader.Read(validationsJson).AsObject();
            
            var tests = validations["validations"].AsArray().Select(x => x.AsObject()).Select(v => new ValidationTest
            {
                Name = v["name"].AsValue().ToString(),
                Transformer = v["transformer"].AsObject(),
                RegisteredTransformers = v["registeredTransformers"]?.AsArray().Select(x => x.AsValue().ToString()).ToArray() ?? Array.Empty<string>(),
                ExpectedIssues = v["expectedIssues"].AsArray().Select(x => x.AsValue().ToString()).ToArray()
            });

            foreach(var test in tests)
            {
                // Each test gets its own context, since registered transformers would otherwise carry over between tests.
                var testContext = CreateContext(testType).RegisterAllTransformers(test.RegisteredTransformers.Select(x => new TransformerRegistration(x, "{}")));
                var validator = new JoltTransformerValidator<IJsonContext>(testContext);
                var issues = validator.Validate(test.Transformer).ToArray();

                if (test.ExpectedIssues.Length == 0)
                {
                    issues.Length.Should().Be(0, "because no issues were expected");
                }
                else
                {
                    issues.Length.Should().Be(test.ExpectedIssues.Length, "because that's the number of issues expected");

                    for (int i = 0; i < test.ExpectedIssues.Length; i++)
                    {
                        ((ExceptionCode)Enum.Parse(typeof(ExceptionCode), test.ExpectedIssues[i])).Should().Be(issues[i].Code, "because that was the expected exception code for this issue");
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetTestData))]
        public void ValidateTransformer_WithValidTransformerType_ShouldHaveNoIssues(TestType testType, string validationsFile)
        {
            ExecuteValidationTests(testType, validationsFile);
        }

        [Theory]
        [MemberData(nameof(GetTestData))]
        public void ValidateTransformer_WithIssues_ShouldLocateEachIssue(TestType testType, string validationsFile)
        {
            var context = CreateContext(testType);
            var transformer = context.JsonTokenReader.Read(@"{
                ""#foreach(@x in @missing) into 'items'"": [ { ""value"": ""@x"" } ],
                ""unknown"": ""#valueOf(@undeclared)"",
                ""broken"": ""#valueOf(""
            }").AsObject();

            var properties = transformer.ToArray();
            var issues = new JoltTransformerValidator<IJsonContext>(context).Validate(transformer).ToArray();

            issues.Length.Should().Be(3, "because each property has one issue");

            issues[0].Code.Should().Be(ExceptionCode.AttemptedToUseUndeclaredVariable, "because @missing is not declared");
            issues[0].TransformerExpressionPath.Should().Be(properties[0].FullPath, "because the issue is in the first property");
            issues[0].IsInPropertyName.Should().Be(true, "because the issue is in the property name");

            issues[1].Code.Should().Be(ExceptionCode.AttemptedToUseUndeclaredVariable, "because @undeclared is not declared");
            issues[1].TransformerExpressionPath.Should().Be(transformer["unknown"].FullPath, "because the issue is in the second property");
            issues[1].IsInPropertyName.Should().Be(false, "because the issue is in the property value");

            issues[2].Type.Should().Be(ValidationIssueType.SyntaxError, "because the expression is incomplete");
            issues[2].TransformerExpressionPath.Should().Be(transformer["broken"].FullPath, "because the issue is in the third property");
            issues[2].IsInPropertyName.Should().Be(false, "because the issue is in the property value");
        }
    }
}
