using FluentAssertions;
using Jolt.Library;
using Jolt.Structure;
using Jolt.Testing.Resources.TestMethods;
using Jolt.Testing.Transformers;
using Jolt.Testing.Transformers.Attributes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Jolt.Testing.Resources.Extensions;

namespace Jolt.Testing.Harness.DotNet
{
    public sealed class TransformationTests : TransformerTests
    {
        [TransformerTest(Transformer.ArrayLiterals, SourceDocument.SingleLevel, typeof(TestContext), TestType.DotNet)]
        public void ArrayLiteral_IsSuccessful_AtSingleLevel(TransformerTest test)
        {
            var json = ExecuteTest(test);

            void AssertThatArrayContains<T>(string arrayName, params T[] values)
            {
                var array = json[arrayName].AsArray();

                array.Length.Should().Be(values?.Length, "because the array should contain the same number of elements as the provided values");

                for(var i = 0; i < array.Length; i++)
                {
                    array[i].ToTypeOf<T>().Should().Be(values[i], $"because that is the value in the source document at index '{i}'");
                }
            }

            AssertThatArrayContains(TargetProperty.ArrayOfIntegerLiterals, 1, 2, 3);
            AssertThatArrayContains(TargetProperty.ArrayOfIntegerValues, 1, 1);
            AssertThatArrayContains(TargetProperty.ArrayOfStringLiterals, "this", "is", "a", "test");
            AssertThatArrayContains(TargetProperty.ArrayOfStringValues, "test", "test");
            AssertThatArrayContains(TargetProperty.ArrayOfBooleanLiterals, true, false, true);
            AssertThatArrayContains(TargetProperty.ArrayOfBooleanValues, true, false, true);
        }

        [TransformerTest(Transformer.SingleLevelValueOf, SourceDocument.SingleLevel, typeof(TestContext), TestType.DotNet)]
        public void ValueOf_IsSuccessful_AtSingleLevelForNumericLiteral(TransformerTest test)
        {
            ValidateLiteralIsTransformed(test, TargetProperty.IntegerLiteral, Value.IntegerLiteral);
        }

        [TransformerTest(Transformer.SingleLevelValueOf, SourceDocument.SingleLevel, typeof(TestContext), TestType.DotNet)]
        public void ValueOf_IsSuccessful_AtSingleLevelForStringLiteral(TransformerTest test)
        {
            ValidateLiteralIsTransformed(test, TargetProperty.StringLiteral, Value.StringLiteral);
        }

        [TransformerTest(Transformer.SingleLevelValueOf, SourceDocument.SingleLevel, typeof(TestContext), TestType.DotNet)]
        public void ValueOf_IsSuccessful_AtSingleLevelForBooleanTrueLiteral(TransformerTest test)
        {
            ValidateLiteralIsTransformed(test, TargetProperty.BooleanTrueLiteral, Value.BooleanTrueLiteral);
        }

        [TransformerTest(Transformer.SingleLevelValueOf, SourceDocument.SingleLevel, typeof(TestContext), TestType.DotNet)]
        public void ValueOf_IsSuccessful_AtSingleLevelForBooleanFalseLiteral(TransformerTest test)
        {
            ValidateLiteralIsTransformed(test, TargetProperty.BooleanFalseLiteral, Value.BooleanFalseLiteral);
        }

        [TransformerTest(Transformer.MultiLevelValueOf, SourceDocument.MultiLevel, typeof(TestContext), TestType.DotNet)]
        public void ValueOf_IsSuccessful_AtMultiLevelForStringLiteral(TransformerTest test)
        {
            var json = ExecuteTest(test);

            var nestedJson = (IJsonObject)json[TargetProperty.Object];

            nestedJson.PropertyValueFor<string>(TargetProperty.StringLiteral).Should().Be(Value.StringLiteral, "because that was the value in the source document");
        }

        [TransformerTest(Transformer.Loops, SourceDocument.Loop, typeof(TestContext), TestType.DotNet)]
        public void Loop_IsSuccessful_AtSingleLevelForMultiElements(TransformerTest test)
        {
            var json = ExecuteTest(test);

            var nestedJson = (IJsonArray)json[TargetProperty.Array];

            nestedJson.Should().NotBeNullOrEmpty("because the transformer contained a template object and the document contained at least one array element");

            var firstElement = (IJsonObject)nestedJson[0];
            var secondElement = (IJsonObject)nestedJson[1];

            firstElement.PropertyValueFor<int>(TargetProperty.ArrayElementId).Should().Be(Value.FirstArrayElementId, "because that was the value in the source document");
            firstElement.PropertyValueFor<int>(TargetProperty.GlobalId).Should().Be(Value.GlobalId, "because that was the value in the source document");
            firstElement.PropertyValueFor<int>(TargetProperty.Index).Should().Be(0, "because this is the index of the first element in the array");

            secondElement.PropertyValueFor<int>(TargetProperty.ArrayElementId).Should().Be(Value.SecondArrayElementId, "because that was the value in the source document");
            secondElement.PropertyValueFor<int>(TargetProperty.GlobalId).Should().Be(Value.GlobalId, "because that was the value in the source document");
            secondElement.PropertyValueFor<int>(TargetProperty.Index).Should().Be(1, "because this is the index of the second element in the array");

            var nestedObject = (IJsonObject)json[TargetProperty.Object];

            nestedObject.Should().NotBeNull("because the transformer looped over properties to build up a new object");

            nestedObject.PropertyValueFor<int>(TargetProperty.First).Should().Be(1, "because that's the value in the related document path");
            nestedObject.PropertyValueFor<int>(TargetProperty.Second).Should().Be(2, "because that's the value in the related document path");

            var arrayFromObject = (IJsonArray)json[TargetProperty.ArrayFromObject];

            arrayFromObject.Should().NotBeNull("because an array should have been created due to the existence of the source object");

            arrayFromObject[0].AsObject()[TargetProperty.First].ToString().Should().Be("1", "because that is the value of the first property in the source object");
            arrayFromObject[1].AsObject()[TargetProperty.Second].ToString().Should().Be("2", "because that is the value of the second property in the source object");

            var objectFromArray = (IJsonObject)json[TargetProperty.ObjectFromArray];

            objectFromArray.Should().NotBeNull("because an object should have been created due to the existence of the source array with elements");

            objectFromArray.PropertyValueFor<int>(TargetProperty.Result).Should().Be(2, "because the last element of the array contained that value in its property");

            var rootObject = (IJsonObject)json[TargetProperty.RootObject];

            rootObject.Should().NotBeNull("because the transformer looped over properties to build up a new object");

            var firstNestedArray = rootObject[TargetProperty.First].AsArray();
            var secondNestedArray = rootObject[TargetProperty.Second].AsArray();

            firstNestedArray.Should().NotBeNull("because this is the name of the first property in the loop iteration");
            secondNestedArray.Should().NotBeNull("because this is the name of the second property in the loop iteration");

            firstNestedArray.ShouldContainProperties(
                (0, "first", 1),
                (1, "first", 2),
                (2, "first", 3)
            );

            firstNestedArray.ShouldContainProperties(
                (0, "testArray[0]", 1),
                (1, "testArray[1]", 1),
                (2, "testArray[2]", 1)
            );

            secondNestedArray.ShouldContainProperties(
                (0, "second", "test1"),
                (1, "second", "test2"),
                (2, "second", "test3")
            );

            secondNestedArray.ShouldContainProperties(
                (0, "testArray[0]", 2),
                (1, "testArray[1]", 2),
                (2, "testArray[2]", 2)
            );
        }

        [TransformerTest(Transformer.MultiLevelValueOf, SourceDocument.MultiLevel, typeof(TestContext), TestType.DotNet)]
        public void ValueOf_WorksSuccessfullyAtMultipleLevels(TransformerTest test)
        {
            var json = ExecuteTest(test);

            json.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

            var objectProperty = (IJsonObject)json[TargetProperty.Object];

            objectProperty.PropertyValueFor<string>(TargetProperty.StringLiteral).Should().Be(Value.StringLiteral, "because that was the value in the source document");
        }

        [TransformerTest(Transformer.Math, SourceDocument.Math, typeof(TestContext), TestType.DotNet)]
        public void Math_IsSuccessful_WithOperatorPrecedence(TransformerTest test)
        {
            var equation = "2 + 3 * 4 + 5 == 19";

            var json = ExecuteTest(test);

            json.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

            json.PropertyValueFor<string>(TargetProperty.Equation).Should().Be(equation, "because this is the value of an equation stored in another document");
            json.PropertyValueFor<string>(TargetProperty.LiteralEquation).Should().Be(equation, "because this is the value of an equation without the context of evaluation");
            json.PropertyValueFor<bool>(TargetProperty.Eval).Should().BeTrue("because this is the result of evaluating an equation");
            json.PropertyValueFor<bool>(TargetProperty.GroupedEquation).Should().BeTrue("because this is the result of evaluating the equation");
            json.PropertyValueFor<double>(TargetProperty.Result).Should().Be(4.123d, "because this is the result of evaluating the equation with mixed types");
            json.PropertyValueFor<bool>(TargetProperty.BooleanTrueLiteral).Should().Be(true, "because this is the result of evaluating the equation");
            json.PropertyValueFor<bool>(TargetProperty.BooleanLiteral).Should().Be(true, "because this is the result of evaluating the equation");
        }

        [TransformerTest(Transformer.Existence, SourceDocument.Existence, typeof(TestContext), TestType.DotNet)]
        public void Existence_IsSuccessful_WithLiteralStringAndNull(TransformerTest test)
        {
            var json = ExecuteTest(test);

            json.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

            json.PropertyValueFor<bool>(TargetProperty.StringLiteral).Should().BeTrue("because the document had content in this property");
            json.PropertyValueFor<bool>(TargetProperty.Empty).Should().BeTrue("because the document had a literal null value in this property");
            json.PropertyValueFor<object?>(TargetProperty.IsString).Should().BeNull("because the value in the document does not exist");
        }

        [TransformerTest(Transformer.Conditions, SourceDocument.Conditions, typeof(TestContext), TestType.DotNet)]
        public void IfCondition_IsSuccessful_WithStringComparisonAsConditionAndStringResult(TransformerTest test)
        {
            var json = ExecuteTest(test);

            json.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

            json.PropertyValueFor<string>(TargetProperty.TrueResult).Should().Be(Value.HasContents, "because the condition evaluated to be true");
            json.PropertyValueFor<string>(TargetProperty.FalseResult).Should().Be(Value.NoContents, "because the condition evaluated to be false");

            var firstObject = (IJsonObject)json[TargetProperty.Object];

            firstObject.Should().NotBeNull("because the condition should have evaluated to true");
            firstObject.PropertyValueFor<string>(TargetProperty.First).Should().Be(Value.Contents, "because its expression read that value");

            var secondObject = (IJsonValue)json[TargetProperty.Result];

            if (secondObject != null)
            {
                secondObject.ToTypeOf<object>().Should().BeNull("because the condition should have evaluated to false");
            }
        }

        [TransformerTest(Transformer.PipedMethods, SourceDocument.PipedMethods, typeof(TestContext), TestType.DotNet)]
        public void PipedMethod_IsSuccessful_WithCallToSingleAndMultiParameterMethod(TransformerTest test)
        {
            var json = ExecuteTest(test);

            json.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

            json.PropertyValueFor<bool>(TargetProperty.TrueResult).Should().BeTrue("because the property exists");
            json.PropertyValueFor<bool>(TargetProperty.FalseResult).Should().BeFalse("because the condition evaluated to be false");
            json.PropertyValueFor<int>(TargetProperty.IntegerLiteral).Should().Be(1, "because the decimal value should truncate during conversion");
            json.PropertyValueFor<string>(TargetProperty.StringLiteral).Should().Be("1.123", "because the decimal value should convert to string");
            json.PropertyValueFor<bool>(TargetProperty.BooleanLiteral).Should().Be(true, "because the string value should convert to boolean");
            json.PropertyValueFor<double>(TargetProperty.DecimalLiteral).Should().Be(1.123d, "because the decimal value should be preserved");
            json.PropertyValueFor<int>(TargetProperty.Length).Should().Be(5, "because that's the number of characters in the source string");
            json.PropertyValueFor<bool>(TargetProperty.StringContains).Should().BeTrue("because the string contains the appropriate value");
            json.PropertyValueFor<double>(TargetProperty.RoundedValue).Should().Be(1.12d, "because the decimal is supposed to be rounded to 2 places");
            json.PropertyValueFor<int>(TargetProperty.Sum).Should().Be(6, "because the array values add up to that number");
            json.PropertyValueFor<string>(TargetProperty.StringJoin).Should().Be("one,two,three", "because the array values should be joined by commas");
            json.PropertyValueFor<string>(TargetProperty.IntegerJoin).Should().Be("1,2,3", "because the array values should be joined by commas");
            json.PropertyValueFor<double>(TargetProperty.Average).Should().Be(2d, "because the array values should be averaged");
            json.PropertyValueFor<double>(TargetProperty.Min).Should().Be(1, "because that's the lowest value in the array");
            json.PropertyValueFor<double>(TargetProperty.Max).Should().Be(3, "because that's the highest value in the array");
            json.PropertyValueFor<bool>(TargetProperty.Empty).Should().BeFalse("because the array has at least one element");
            json.PropertyValueFor<bool>(TargetProperty.Any).Should().BeTrue("because the array has at least one element");
            json.PropertyValueFor<bool>(TargetProperty.IsInteger).Should().Be(true, "because that's the type of the value");
            json.PropertyValueFor<bool>(TargetProperty.IsString).Should().Be(true, "because that's the type of the value");
            json.PropertyValueFor<bool>(TargetProperty.IsDecimal).Should().Be(true, "because that's the type of the value");
            json.PropertyValueFor<bool>(TargetProperty.IsBoolean).Should().Be(true, "because that's the type of the value");
            json.PropertyValueFor<bool>(TargetProperty.IsArray).Should().Be(true, "because that's the type of the value");
            json.PropertyValueFor<int>(TargetProperty.Index).Should().Be(2, "because that's the first index of the value in the string");
            json[TargetProperty.Array].AsArray().ShouldContain("1", "2", "3");
            json.PropertyValueFor<string>(TargetProperty.AppendedString).Should().Be("1.1231,2,3", "because the strings should be concatenated together");
            json.PropertyValueFor<string>(TargetProperty.AppendedVariadic).Should().Be("1.1231,2,31,2,3", "because the strings should be concatenated together");
            json[TargetProperty.AppendedArray].AsArray().ShouldContain("one", "two", "three", "one", "two", "three");
            json[TargetProperty.AppendedObject].AsObject().ShouldContainProperties(("first", 1), ("second", 2));
            json[TargetProperty.Group].AsArray().ShouldContainProperties((0, "key", "one"), (1, "key", "two"));
            json[TargetProperty.Summary].AsArray().ShouldContainProperties((0, "one", 6), (1, "two", 2));
            json[TargetProperty.Order].AsArray().ShouldContainProperties((0, "type", "one"), (1, "type", "one"), (2, "type", "two"));
            json[TargetProperty.Order1].AsArray().ShouldContain(1, 2, 3);
            json[TargetProperty.OrderDesc].AsArray().ShouldContainProperties((0, "type", "two"), (1, "type", "one"), (2, "type", "one"));
            json[TargetProperty.OrderDesc1].AsArray().ShouldContain(3, 2, 1);
            json.PropertyValueFor<string>(TargetProperty.Substring1).Should().Be(".1", "because that is the substring that should be retrieved from the source document");
            json.PropertyValueFor<string>(TargetProperty.Substring2).Should().Be(".123", "because that is the substring that should be retrieved from the source document");
            json.PropertyValueFor<string>(TargetProperty.Substring3).Should().Be("1.1", "because that is the substring that should be retrieved from the source document");
            json.PropertyValueFor<string>(TargetProperty.Substring4).Should().Be("3", "because that is the substring that should be retrieved from the source document");
            json.PropertyValueFor<string>(TargetProperty.Substring5).Should().Be(".", "because that is the substring that should be retrieved from the source document");
            json.PropertyValueFor<string>(TargetProperty.Substring6).Should().Be("1.", "because that is the substring that should be retrieved from the source document");
        }

        [TransformerTest(Transformer.ExternalMethods, SourceDocument.ExternalMethods, typeof(TestContext), TestType.DotNet, typeof(ExternalInstanceMethods))]
        public void ExternalMethod_IsSuccessful_WithStaticAndInstanceCallToSingleAndMultiParameterMethod(TransformerTest test)
        {
            var staticBoolRegistration = MethodRegistration.FromStaticMethod(typeof(ExternalStaticMethods), nameof(ExternalStaticMethods.TakesAndReturnsBoolean));
            var staticConcatRegistration = MethodRegistration.FromStaticMethod(typeof(ExternalStaticMethods), nameof(ExternalStaticMethods.Concatenate), "ConcatAlias");

            var instanceAppendRegistration = MethodRegistration.FromInstanceMethod(nameof(ExternalInstanceMethods.AppendString));

            var externalMethods = new[] { staticBoolRegistration, staticConcatRegistration, instanceAppendRegistration };

            var json = ExecuteTest(test, x => x.RegisterAllMethods(externalMethods));

            json.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

            json.PropertyValueFor<bool>(TargetProperty.BooleanLiteral).Should().BeTrue("because that is the value in the source document");
            json.PropertyValueFor<string>(TargetProperty.StringLiteral).Should().Be("testtest", "because that is the value concatenated with itself in the source document");
            json.PropertyValueFor<string>(TargetProperty.AppendedString).Should().Be("testtest", "because that is the value appended twice with itself in the source document");
        }

        [TransformerTest("ExternalMethodsWithAliases", "ExternalMethodsDocument", typeof(TestContext), TestType.DotNet, typeof(ExternalMixedMethods))]
        public void ExternalMethod_IsSuccessful_UsingDefaultWithStaticMethodAliasedRegistrations(TransformerTest test)
        {
            var json = ExecuteTest(test);

            json.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

            json.PropertyValueFor<bool>(TargetProperty.BooleanLiteral).Should().BeTrue("because that is the value in the source document");
            json.PropertyValueFor<string>(TargetProperty.StringLiteral).Should().Be("testtest", "because that is the value concatenated with itself in the source document");
            json.PropertyValueFor<string>(TargetProperty.AppendedString).Should().Be("testtest", "because that is the value appended twice with itself in the source document");
        }

        [TransformerTest(Transformer.RangeVariables, SourceDocument.Loop, typeof(TestContext), TestType.DotNet)]
        public void RangeVariables_AreSuccessful_WithDeclarationAndUsage(TransformerTest test)
        {
            var json = ExecuteTest(test);

            json.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

            json.PropertyValueFor<int>(TargetProperty.IntegerLiteral).Should().Be(3, "because that's the value in the source document");
            json.PropertyValueFor<int>(TargetProperty.Result).Should().Be(5, "because that's the result value of the equation");

            json["Temp3Copy"].AsArray().ShouldContainProperties((0, "ElementId", 1), (1, "ElementId", 2));
            json["ActualTemp3"].AsArray().ShouldContainProperties((0, "ActualElementId", 2), (1, "ActualElementId", 4));
            json.PropertyValueFor<string>("InvalidVarReference").Should().BeNull("because the lifetime of the scoped variable ended with the loop");
        }

        [TransformerTest(Transformer.Lambdas, SourceDocument.Lambdas, typeof(TestContext), TestType.DotNet)]
        public void Lambdas_AreSuccessful_WithDeclarationAndUsage(TransformerTest test)
        {
            var json = ExecuteTest(test);

            json.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

            json["Any"].ToTypeOf<bool>().Should().BeTrue("because there is at least one property that matches the query");
            json["Filtered"].AsArray().ShouldContainProperties((0, "arrayElementId", 1));
            json["Selected"].AsArray().ShouldContain(1, 2);
        }

        [TransformerTest(Transformer.UsingBlock, SourceDocument.Loop, typeof(TestContext), TestType.DotNet)]
        public void UsingBlock_IsSuccessful_WithDeclarationAndUsageIncludingFromRangeVariables(TransformerTest test)
        {
            var json = ExecuteTest(test);

            json.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

            var obj = json[TargetProperty.Result].AsObject();

            obj.Should().NotBeNull("because a valid object was assigned in the transformer");
            obj[TargetProperty.First].Should().BeNull("because the property was removed in the transformer");
            obj[TargetProperty.Second].AsObject().PropertyValueFor<object>(SourceProperty.Value).Should().BeNull("because the value was removed in the source document");

            obj.SelectTokenAtPath("$.third.deep.value").ToTypeOf<int>().Should().Be(3, "because that's the value assigned in the transformer");
            obj.SelectTokenAtPath("$.fourth.id").ToTypeOf<int>().Should().Be(12, "because that's the result of the equation in the transformer");
        }

        private void ValidateLiteralIsTransformed<T>(TransformerTest test, string targetProperty, T targetValue, [CallerMemberName] string testMethodName = default)
        {
            var json = ExecuteTest(test, testMethodName: testMethodName);

            json.PropertyValueFor<T>(targetProperty).Should().Be(targetValue, "because that was the value in the source document");
        }
    }
}
