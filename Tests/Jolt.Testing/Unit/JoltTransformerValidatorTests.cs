using Jolt.Testing.Assertions;
using Jolt.Exceptions;
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
                ExpectedIssues = v["expectedIssues"].AsArray().Select(x => x.AsValue().ToString()).ToArray()
            });

            var validator = new JoltTransformerValidator<IJsonContext>(context);

            foreach(var test in tests)
            {
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
    }
}
