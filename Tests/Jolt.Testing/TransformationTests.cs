using FluentAssertions;
using Jolt.Library;
using Jolt.Structure;
using Jolt.Testing.Assertions;
using Jolt.Testing.Json;
using Jolt.Testing.Resources.Extensions;
using Jolt.Testing.Resources.TestMethods;
using Jolt.Testing.Transformers;
using Jolt.Testing.Transformers.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Jolt.Testing.TestContainer;

namespace Jolt.Testing
{
    public abstract class TransformationTests : TransformerTests
    {
        public virtual async Task ReaderWriterTransformations_AreSuccessful(TransformerTest test)
        {
            var synchronousJson = await ExecuteTest(test, executionType: TestExecutionType.SynchronousReaderWriter);
            var asynchronousJson = await ExecuteTest(test, executionType: TestExecutionType.AsynchronousReaderWriter);

            synchronousJson.AsArray().ExpectsContentsEqualTo(asynchronousJson.AsArray(), "because both synchronous and asynchronous transformations should produce the same result");
        }

        public virtual async Task StreamingTransformations_AreSuccessful(TransformerTest test)
        {
            var synchronousJson = await ExecuteTest(test, executionType: TestExecutionType.SynchronousStream);
            var asynchronousJson = await ExecuteTest(test, executionType: TestExecutionType.AsynchronousStream);

            synchronousJson.AsArray().ExpectsContentsEqualTo(asynchronousJson.AsArray(), "because both synchronous and asynchronous transformations should produce the same result");
        }

        public virtual async Task PartialTransformerReference_IsSuccessful(TransformerTest test)
        {
            var json = await ExecuteTestForObjectResult(test, partialTransformerNames: Transformer.PartialTransformer);

            json.ExpectsNonNull("because a valid document was sent in and used by a valid transformer");

            var reference = json["transformerReference"] as IJsonObject;

            reference.ExpectsNonNull("because the transformer should have been included and executed successfully");
            reference.ExpectsPropertyNull<object>(TargetProperty.SourceVariableX, "because the secondary transformer does not have access to its callers variables");
            reference.ExpectsPropertyEqualTo(TargetProperty.SourcePathValue, "test.source.path", "because that is the value at the source path in the selected document sub-path");
            reference.ExpectsPropertyNull<object>(TargetProperty.IntegerValue, "because no parameters were provided to the secondary transformer");
            reference.ExpectsPropertyNull<string>(TargetProperty.TextValue, "because no parameters were provided to the secondary transformer");

            var transformerWithParams = json["transformerWithParams"] as IJsonObject;

            transformerWithParams.ExpectsNonNull("because the transformer should have been included and executed successfully");
            transformerWithParams.ExpectsPropertyNull<object>(TargetProperty.SourceVariableX, "because the secondary transformer does not have access to its callers variables");
            transformerWithParams.ExpectsPropertyEqualTo(TargetProperty.SourcePathValue, "test.source.path", "because that is the value at the source path in the selected document sub-path");
            transformerWithParams.ExpectsPropertyNonNull<object>(TargetProperty.IntegerValue, "because that parameter was provided to the secondary transformer");
            transformerWithParams.ExpectsPropertyNonNull<string>(TargetProperty.TextValue, "because that parameter was provided to the secondary transformer");
        }

        public virtual async Task ArrayLiteral_IsSuccessful_AtSingleLevel(TransformerTest test)
        {
            var json = await ExecuteTestForObjectResult(test);

            json.ExpectsArrayContains(TargetProperty.ArrayOfIntegerLiterals, 1, 2, 3);
            json.ExpectsArrayContains(TargetProperty.ArrayOfIntegerValues, 1, 1);
            json.ExpectsArrayContains(TargetProperty.ArrayOfStringLiterals, "this", "is", "a", "test");
            json.ExpectsArrayContains(TargetProperty.ArrayOfStringValues, "test", "test");
            json.ExpectsArrayContains(TargetProperty.ArrayOfBooleanLiterals, true, false, true);
            json.ExpectsArrayContains(TargetProperty.ArrayOfBooleanValues, true, false, true);
        }

        public virtual async Task ValueOf_IsSuccessful_AtSingleLevelForNumericLiteral(TransformerTest test)
        {
            await ValidateLiteralIsTransformed(test, TargetProperty.IntegerLiteral, Value.IntegerLiteral);
        }

        public virtual async Task ValueOf_IsSuccessful_AtSingleLevelForStringLiteral(TransformerTest test)
        {
            await ValidateLiteralIsTransformed(test, TargetProperty.StringLiteral, Value.StringLiteral);
        }

        public virtual async Task ValueOf_IsSuccessful_AtSingleLevelForBooleanTrueLiteral(TransformerTest test)
        {
            await ValidateLiteralIsTransformed(test, TargetProperty.BooleanTrueLiteral, Value.BooleanTrueLiteral);
        }

        public virtual async Task ValueOf_IsSuccessful_AtSingleLevelForBooleanFalseLiteral(TransformerTest test)
        {
            await ValidateLiteralIsTransformed(test, TargetProperty.BooleanFalseLiteral, Value.BooleanFalseLiteral);
        }

        public virtual async Task ValueOf_IsSuccessful_AtMultiLevelForStringLiteral(TransformerTest test)
        {
            var json = await ExecuteTestForObjectResult(test);

            var nestedJson = (IJsonObject)json[TargetProperty.Object];

            nestedJson.ExpectsPropertyEqualTo(TargetProperty.StringLiteral, Value.StringLiteral, "because that was the value in the source document");
        }

        public virtual async Task Loop_IsSuccessful_AtSingleLevelForMultiElements(TransformerTest test)
        {
            var json = await ExecuteTestForObjectResult(test);

            var nestedJson = (IJsonArray)json[TargetProperty.Array];

            nestedJson.ExpectsArrayNonNullAndNonEmpty("because the transformer contained a template object and the document contained at least one array element");

            var firstElement = (IJsonObject)nestedJson[0];
            var secondElement = (IJsonObject)nestedJson[1];

            firstElement.ExpectsPropertyEqualTo(TargetProperty.ArrayElementId, Value.FirstArrayElementId, "because that was the value in the source document");
            firstElement.ExpectsPropertyEqualTo(TargetProperty.GlobalId, Value.GlobalId, "because that was the value in the source document");
            firstElement.ExpectsPropertyEqualTo(TargetProperty.Index, 0, "because this is the index of the first element in the array");

            secondElement.ExpectsPropertyEqualTo(TargetProperty.ArrayElementId, Value.SecondArrayElementId, "because that was the value in the source document");
            secondElement.ExpectsPropertyEqualTo(TargetProperty.GlobalId, Value.GlobalId, "because that was the value in the source document");
            secondElement.ExpectsPropertyEqualTo(TargetProperty.Index, 1, "because this is the index of the second element in the array");

            var nestedObject = (IJsonObject)json[TargetProperty.Object];

            nestedObject.ExpectsNonNull("because the transformer looped over properties to build up a new object");

            nestedObject.ExpectsPropertyEqualTo(TargetProperty.First, 1, "because that's the value in the related document path");
            nestedObject.ExpectsPropertyEqualTo(TargetProperty.Second, 2, "because that's the value in the related document path");

            var arrayFromObject = (IJsonArray)json[TargetProperty.ArrayFromObject];

            arrayFromObject.ExpectsNonNull("because an array should have been created due to the existence of the source object");

            arrayFromObject[0].AsObject()[TargetProperty.First].ToString().ExpectsEqualTo("1", "because that is the value of the first property in the source object");
            arrayFromObject[1].AsObject()[TargetProperty.Second].ToString().ExpectsEqualTo("2", "because that is the value of the second property in the source object");

            var objectFromArray = (IJsonObject)json[TargetProperty.ObjectFromArray];

            objectFromArray.ExpectsNonNull("because an object should have been created due to the existence of the source array with elements");

            objectFromArray.ExpectsPropertyEqualTo(TargetProperty.Result, 2, "because the last element of the array contained that value in its property");

            var rootObject = (IJsonObject)json[TargetProperty.RootObject];

            rootObject.ExpectsNonNull("because the transformer looped over properties to build up a new object");

            var firstNestedArray = rootObject[TargetProperty.First].AsArray();
            var secondNestedArray = rootObject[TargetProperty.Second].AsArray();

            firstNestedArray.ExpectsNonNull("because this is the name of the first property in the loop iteration");
            secondNestedArray.ExpectsNonNull("because this is the name of the second property in the loop iteration");

            firstNestedArray.ExpectsContainsProperties(
                (0, "first", 1),
                (1, "first", 2),
                (2, "first", 3)
            );

            firstNestedArray.ExpectsContainsProperties(
                (0, "testArray[0]", 1),
                (1, "testArray[1]", 1),
                (2, "testArray[2]", 1)
            );

            secondNestedArray.ExpectsContainsProperties(
                (0, "second", "test1"),
                (1, "second", "test2"),
                (2, "second", "test3")
            );

            secondNestedArray.ExpectsContainsProperties(
                (0, "testArray[0]", 2),
                (1, "testArray[1]", 2),
                (2, "testArray[2]", 2)
            );
        }

        public virtual async Task ValueOf_WorksSuccessfullyAtMultipleLevels(TransformerTest test)
        {
            var json = await ExecuteTestForObjectResult(test);

            json.ExpectsNonNull("because a valid document was sent in and used by a valid transformer");

            var objectProperty = (IJsonObject)json[TargetProperty.Object];

            objectProperty.ExpectsPropertyEqualTo(TargetProperty.StringLiteral, Value.StringLiteral, "because that was the value in the source document");
        }

        public virtual async Task Math_IsSuccessful_WithOperatorPrecedence(TransformerTest test)
        {
            var equation = "2 + 3 * 4 + 5 == 19";

            var json = await ExecuteTestForObjectResult(test);

            json.ExpectsNonNull("because a valid document was sent in and used by a valid transformer");

            json.ExpectsPropertyEqualTo(TargetProperty.Equation, equation, "because this is the value of an equation stored in another document");
            json.ExpectsPropertyEqualTo(TargetProperty.LiteralEquation, equation, "because this is the value of an equation without the context of evaluation");
            json.ExpectsPropertyEqualTo(TargetProperty.Eval, true, "because this is the result of evaluating an equation");
            json.ExpectsPropertyEqualTo(TargetProperty.GroupedEquation, true, "because this is the result of evaluating the equation");
            json.ExpectsPropertyEqualTo(TargetProperty.Result, 4.123d, "because this is the result of evaluating the equation with mixed types");
            json.ExpectsPropertyEqualTo(TargetProperty.BooleanTrueLiteral, true, "because this is the result of evaluating the equation");
            json.ExpectsPropertyEqualTo(TargetProperty.BooleanLiteral, true, "because this is the result of evaluating the equation");
        }

        public virtual async Task Existence_IsSuccessful_WithLiteralStringAndNull(TransformerTest test)
        {
            var json = await ExecuteTestForObjectResult(test);

            json.ExpectsNonNull("because a valid document was sent in and used by a valid transformer");

            json.ExpectsPropertyEqualTo(TargetProperty.StringLiteral, true, "because the document had content in this property");
            json.ExpectsPropertyEqualTo(TargetProperty.Empty, true, "because the document had a literal null value in this property");
            json.ExpectsPropertyNull<object>(TargetProperty.IsString, "because the value in the document does not exist");
        }

        public virtual async Task IfCondition_IsSuccessful_WithStringComparisonAsConditionAndStringResult(TransformerTest test)
        {
            var json = await ExecuteTestForObjectResult(test);

            json.ExpectsNonNull("because a valid document was sent in and used by a valid transformer");

            json.ExpectsPropertyEqualTo(TargetProperty.TrueResult, Value.HasContents, "because the condition evaluated to be true");
            json.ExpectsPropertyEqualTo(TargetProperty.FalseResult, Value.NoContents, "because the condition evaluated to be false");

            var firstObject = (IJsonObject)json[TargetProperty.Object];

            firstObject.ExpectsNonNull("because the condition should have evaluated to true");
            firstObject.ExpectsPropertyEqualTo(TargetProperty.First, Value.Contents, "because its expression read that value");

            var secondObject = (IJsonValue)json[TargetProperty.Result];

            if (secondObject != null)
            {
                secondObject.ToTypeOf<object>().ExpectsEqualTo(null, "because the condition should have evaluated to false");
            }
        }

        public virtual async Task PipedMethod_IsSuccessful_WithCallToSingleAndMultiParameterMethod(TransformerTest test)
        {
            var json = await ExecuteTestForObjectResult(test);

            json.ExpectsNonNull("because a valid document was sent in and used by a valid transformer");

            json.ExpectsPropertyEqualTo(TargetProperty.TrueResult, true, "because the property exists");
            json.ExpectsPropertyEqualTo(TargetProperty.FalseResult, false, "because the condition evaluated to be false");
            json.ExpectsPropertyEqualTo(TargetProperty.IntegerLiteral, 1, "because the decimal value should truncate during conversion");
            json.ExpectsPropertyEqualTo(TargetProperty.StringLiteral, "1.123", "because the decimal value should convert to string");
            json.ExpectsPropertyEqualTo(TargetProperty.BooleanLiteral, true, "because the string value should convert to boolean");
            json.ExpectsPropertyEqualTo(TargetProperty.DecimalLiteral, 1.123d, "because the decimal value should be preserved");
            json.ExpectsPropertyEqualTo(TargetProperty.Length, 5, "because that's the number of characters in the source string");
            json.ExpectsPropertyEqualTo(TargetProperty.StringContains, true, "because the string contains the appropriate value");
            json.ExpectsPropertyEqualTo(TargetProperty.RoundedValue, 1.12d, "because the decimal is supposed to be rounded to 2 places");
            json.ExpectsPropertyEqualTo(TargetProperty.Sum, 6, "because the array values add up to that number");
            json.ExpectsPropertyEqualTo(TargetProperty.StringJoin, "one,two,three", "because the array values should be joined by commas");
            json.ExpectsPropertyEqualTo(TargetProperty.IntegerJoin, "1,2,3", "because the array values should be joined by commas");
            json.ExpectsPropertyEqualTo(TargetProperty.Average, 2d, "because the array values should be averaged");
            json.ExpectsPropertyEqualTo(TargetProperty.Min, 1, "because that's the lowest value in the array");
            json.ExpectsPropertyEqualTo(TargetProperty.Max, 3, "because that's the highest value in the array");
            json.ExpectsPropertyEqualTo(TargetProperty.Empty, false, "because the array has at least one element");
            json.ExpectsPropertyEqualTo(TargetProperty.Any, true, "because the array has at least one element");
            json.ExpectsPropertyEqualTo(TargetProperty.IsInteger, true, "because that's the type of the value");
            json.ExpectsPropertyEqualTo(TargetProperty.IsString, true, "because that's the type of the value");
            json.ExpectsPropertyEqualTo(TargetProperty.IsDecimal, true, "because that's the type of the value");
            json.ExpectsPropertyEqualTo(TargetProperty.IsBoolean, true, "because that's the type of the value");
            json.ExpectsPropertyEqualTo(TargetProperty.IsArray, true, "because that's the type of the value");
            json.ExpectsPropertyEqualTo(TargetProperty.Index, 2, "because that's the first index of the value in the string");
            json[TargetProperty.Array].AsArray().ExpectsContains("1", "2", "3");
            json.ExpectsPropertyEqualTo(TargetProperty.AppendedString, "1.1231,2,3", "because the strings should be concatenated together");
            json.ExpectsPropertyEqualTo(TargetProperty.AppendedVariadic, "1.1231,2,31,2,3", "because the strings should be concatenated together");
            json[TargetProperty.AppendedArray].AsArray().ExpectsContains("one", "two", "three", "one", "two", "three");
            json[TargetProperty.AppendedObject].AsObject().ExpectsContainsProperties(("first", 1), ("second", 2));
            json[TargetProperty.Group].AsArray().ExpectsContainsProperties((0, "key", "one"), (1, "key", "two"));
            json[TargetProperty.Summary].AsArray().ExpectsContainsProperties((0, "one", 6), (1, "two", 2));
            json[TargetProperty.Order].AsArray().ExpectsContainsProperties((0, "type", "one"), (1, "type", "one"), (2, "type", "two"));
            json[TargetProperty.Order1].AsArray().ExpectsContains(1, 2, 3);
            json[TargetProperty.OrderDesc].AsArray().ExpectsContainsProperties((0, "type", "two"), (1, "type", "one"), (2, "type", "one"));
            json[TargetProperty.OrderDesc1].AsArray().ExpectsContains(3, 2, 1);
            json.ExpectsPropertyEqualTo(TargetProperty.Substring1, ".1", "because that is the substring that should be retrieved from the source document");
            json.ExpectsPropertyEqualTo(TargetProperty.Substring2, ".123", "because that is the substring that should be retrieved from the source document");
            json.ExpectsPropertyEqualTo(TargetProperty.Substring3, "1.1", "because that is the substring that should be retrieved from the source document");
            json.ExpectsPropertyEqualTo(TargetProperty.Substring4, "3", "because that is the substring that should be retrieved from the source document");
            json.ExpectsPropertyEqualTo(TargetProperty.Substring5, ".", "because that is the substring that should be retrieved from the source document");
            json.ExpectsPropertyEqualTo(TargetProperty.Substring6, "1.", "because that is the substring that should be retrieved from the source document");
        }

        public virtual async Task ExternalMethod_IsSuccessful_WithStaticAndInstanceCallToSingleAndMultiParameterMethod(TransformerTest test)
        {
            var staticBoolRegistration = MethodRegistration.FromStaticMethod(typeof(ExternalStaticMethods), nameof(ExternalStaticMethods.TakesAndReturnsBoolean));
            var staticConcatRegistration = MethodRegistration.FromStaticMethod(typeof(ExternalStaticMethods), nameof(ExternalStaticMethods.Concatenate), "ConcatAlias");

            var instanceAppendRegistration = MethodRegistration.FromInstanceMethod(nameof(ExternalInstanceMethods.AppendString));

            var externalMethods = new[] { staticBoolRegistration, staticConcatRegistration, instanceAppendRegistration };

            var json = await ExecuteTestForObjectResult(test, x => x.RegisterAllMethods(externalMethods));

            json.ExpectsNonNull("because a valid document was sent in and used by a valid transformer");

            json.ExpectsPropertyEqualTo(TargetProperty.BooleanLiteral, true, "because that is the value in the source document");
            json.ExpectsPropertyEqualTo(TargetProperty.StringLiteral, "testtest", "because that is the value concatenated with itself in the source document");
            json.ExpectsPropertyEqualTo(TargetProperty.AppendedString, "testtest", "because that is the value appended twice with itself in the source document");
        }

        public virtual async Task ExternalMethod_IsSuccessful_UsingDefaultWithStaticMethodAliasedRegistrations(TransformerTest test)
        {
            var json = await ExecuteTestForObjectResult(test);

            json.ExpectsNonNull("because a valid document was sent in and used by a valid transformer");

            json.ExpectsPropertyEqualTo(TargetProperty.BooleanLiteral, true, "because that is the value in the source document");
            json.ExpectsPropertyEqualTo(TargetProperty.StringLiteral, "testtest", "because that is the value concatenated with itself in the source document");
            json.ExpectsPropertyEqualTo(TargetProperty.AppendedString, "testtest", "because that is the value appended twice with itself in the source document");
        }

        public virtual async Task RangeVariables_AreSuccessful_WithDeclarationAndUsage(TransformerTest test)
        {
            var json = await ExecuteTestForObjectResult(test);

            json.ExpectsNonNull("because a valid document was sent in and used by a valid transformer");

            json.ExpectsPropertyEqualTo(TargetProperty.IntegerLiteral, 3, "because that's the value in the source document");
            json.ExpectsPropertyEqualTo(TargetProperty.Result, 5, "because that's the result value of the equation");

            json["Temp3Copy"].AsArray().ExpectsContainsProperties((0, "ElementId", 1), (1, "ElementId", 2));
            json["ActualTemp3"].AsArray().ExpectsContainsProperties((0, "ActualElementId", 2), (1, "ActualElementId", 4));
            json.ExpectsPropertyNull<object>("InvalidVarReference", "because the lifetime of the scoped variable ended with the loop");
        }

        public virtual async Task Lambdas_AreSuccessful_WithDeclarationAndUsage(TransformerTest test)
        {
            var json = await ExecuteTestForObjectResult(test);

            json.ExpectsNonNull("because a valid document was sent in and used by a valid transformer");

            json["Any"].ToTypeOf<bool>().ExpectsEqualTo(true, "because there is at least one property that matches the query");
            json["Filtered"].AsArray().ExpectsContainsProperties((0, "arrayElementId", 1));
            json["Selected"].AsArray().ExpectsContains(1, 2);
        }

        public virtual async Task UsingBlock_IsSuccessful_WithDeclarationAndUsageIncludingFromRangeVariables(TransformerTest test)
        {
            var json = await ExecuteTestForObjectResult(test);

            json.ExpectsNonNull("because a valid document was sent in and used by a valid transformer");

            var obj = json[TargetProperty.Result].AsObject();

            obj.ExpectsNonNull("because a valid object was assigned in the transformer");
            obj.ExpectsPropertyNull<object>(TargetProperty.First, "because the property was removed in the transformer");
            obj[TargetProperty.Second].AsObject().ExpectsPropertyNull<object>(SourceProperty.Value, "because the value was removed in the source document");

            obj.SelectTokenAtPath("$.third.deep.value").ToTypeOf<int>().ExpectsEqualTo(3, "because that's the value assigned in the transformer");
            obj.SelectTokenAtPath("$.fourth.id").ToTypeOf<int>().ExpectsEqualTo(12, "because that's the result of the equation in the transformer");
        }

        private async Task ValidateLiteralIsTransformed<T>(TransformerTest test, string targetProperty, T targetValue, [CallerMemberName] string testMethodName = default)
        {
            var json = await ExecuteTestForObjectResult(test, testMethodName: testMethodName);

            json.PropertyValueFor<T>(targetProperty).Should().Be(targetValue, "because that was the value in the source document");
        }
    }
}
