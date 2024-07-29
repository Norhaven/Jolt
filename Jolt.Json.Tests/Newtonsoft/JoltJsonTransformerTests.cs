using FluentAssertions;
using Jolt.Json.Newtonsoft;
using Jolt.Json.Tests.Newtonsoft.Extensions;
using Jolt.Json.Tests.TestMethods;
using Jolt.Library;
using Jolt.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.Newtonsoft;

public class JoltJsonTransformerTests : Test
{
    protected override IJsonTokenReader CreateTokenReader() => new JsonTokenReader();
    protected override IJsonTransformer<IJsonContext> CreateTransformer(JoltContext context) => new JoltJsonTransformer(context);
    protected override IQueryPathProvider CreateQueryPathProvider() => new JsonPathQueryPathProvider();

    [Fact]
    public void ValueOf_IsSuccessful_AtSingleLevelForNumericLiteral()
    {
        ValidateLiteralIsTransformed(_singleLevelValueOf, _singleLevelDocument, TargetProperty.IntegerLiteral, Value.IntegerLiteral);
    }

    [Fact]
    public void ValueOf_IsSuccessful_AtSingleLevelForStringLiteral()
    {
        ValidateLiteralIsTransformed(_singleLevelValueOf, _singleLevelDocument, TargetProperty.StringLiteral, Value.StringLiteral);
    }

    [Fact]
    public void ValueOf_IsSuccessful_AtSingleLevelForBooleanTrueLiteral()
    {
        ValidateLiteralIsTransformed(_singleLevelValueOf, _singleLevelDocument, TargetProperty.BooleanTrueLiteral, Value.BooleanTrueLiteral);
    }

    [Fact]
    public void ValueOf_IsSuccessful_AtSingleLevelForBooleanFalseLiteral()
    {
        ValidateLiteralIsTransformed(_singleLevelValueOf, _singleLevelDocument, TargetProperty.BooleanFalseLiteral, Value.BooleanFalseLiteral);
    }

    [Fact]
    public void ValueOf_IsSuccessful_AtMultiLevelForStringLiteral()
    {
        var json = ExecuteTestFor(_multiLevelValueOf, _multiLevelDocument);

        var nestedJson = (JObject)json[TargetProperty.Object];

        nestedJson.PropertyValueFor<string>(TargetProperty.StringLiteral).Should().Be(Value.StringLiteral, "because that was the value in the source document");
    }

    [Fact]
    public void Loop_IsSuccessful_AtSingleLevelForMultiElements()
    {
        var json = ExecuteTestFor(_singleLevelLoop, _singleLevelLoopDocument);

        var nestedJson = (JArray)json[TargetProperty.Array];

        nestedJson.Should().NotBeNullOrEmpty("because the transformer contained a template object and the document contained at least one array element");

        var firstElement = (JObject)nestedJson[0];
        var secondElement = (JObject)nestedJson[1];

        firstElement.PropertyValueFor<int>(TargetProperty.ArrayElementId).Should().Be(Value.FirstArrayElementId, "because that was the value in the source document");
        firstElement.PropertyValueFor<int>(TargetProperty.GlobalId).Should().Be(Value.GlobalId, "because that was the value in the source document");
        firstElement.PropertyValueFor<int>(TargetProperty.Index).Should().Be(0, "because this is the index of the first element in the array");

        secondElement.PropertyValueFor<int>(TargetProperty.ArrayElementId).Should().Be(Value.SecondArrayElementId, "because that was the value in the source document");
        secondElement.PropertyValueFor<int>(TargetProperty.GlobalId).Should().Be(Value.GlobalId, "because that was the value in the source document");
        secondElement.PropertyValueFor<int>(TargetProperty.Index).Should().Be(1, "because this is the index of the second element in the array");

        var nestedObject = (JObject)json[TargetProperty.Object];

        nestedObject.Should().NotBeNull("because the transformer looped over properties to build up a new object");

        nestedObject.PropertyValueFor<int>(TargetProperty.First).Should().Be(1, "because that's the value in the related document path");
        nestedObject.PropertyValueFor<int>(TargetProperty.Second).Should().Be(2, "because that's the value in the related document path");

        var arrayFromObject = (JArray)json[TargetProperty.ArrayFromObject];

        arrayFromObject.Should().NotBeNull("because an array should have been created due to the existence of the source object");

        arrayFromObject[0][TargetProperty.First].ToString().Should().Be("1", "because that is the value of the first property in the source object");
        arrayFromObject[1][TargetProperty.Second].ToString().Should().Be("2", "because that is the value of the first property in the source object");

        var objectFromArray = (JObject)json[TargetProperty.ObjectFromArray];

        objectFromArray.Should().NotBeNull("because an object should have been created due to the existence of the source array with elements");

        objectFromArray.PropertyValueFor<int>(TargetProperty.Result).Should().Be(2, "because the last element of the array contained that value in its property");
    }

    [Fact]
    public void ValueOf_WorksSuccessfullyAtMultipleLevels()
    {
        var json = ExecuteTestFor(_multiLevelValueOf, _multiLevelDocument);
        
        json.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

        var objectProperty = (JObject)json[TargetProperty.Object];

        objectProperty.PropertyValueFor<string>(TargetProperty.StringLiteral).Should().Be(Value.StringLiteral, "because that was the value in the source document");
    }

    [Fact]
    public void Math_IsSuccessful_WithOperatorPrecedence()
    {
        var equation = "2 + 3 * 4 + 5 = 19";

        var json = ExecuteTestFor(_math, _mathDocument);

        json.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

        json.PropertyValueFor<string>(TargetProperty.Equation).Should().Be(equation, "because this is the value of an equation stored in another document");
        json.PropertyValueFor<string>(TargetProperty.LiteralEquation).Should().Be(equation, "because this is the value of an equation without the context of evaluation");
        json.PropertyValueFor<bool>(TargetProperty.Eval).Should().BeTrue("because this is the result of evaluating an equation");
        json.PropertyValueFor<bool>(TargetProperty.GroupedEquation).Should().BeTrue("because this is the result of evaluating the equation");
        json.PropertyValueFor<double>(TargetProperty.Result).Should().Be(4.123d, "because this is the result of evaluating the equation with mixed types");
    }

    [Fact]
    public void Existence_IsSuccessful_WithLiteralStringAndNull()
    {
        var json = ExecuteTestFor(_existence, _existenceDocument);

        json.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

        json.PropertyValueFor<bool>(TargetProperty.StringLiteral).Should().BeTrue("because the document had content in this property");
        json.PropertyValueFor<bool>(TargetProperty.Empty).Should().BeFalse("because the document had a null value in this property");
    }

    [Fact]
    public void IfCondition_IsSuccessful_WithStringComparisonAsConditionAndStringResult()
    {
        var json = ExecuteTestFor(_conditions, _conditionsDocument);

        json.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

        json.PropertyValueFor<string>(TargetProperty.TrueResult).Should().Be(Value.HasContents, "because the condition evaluated to be true");
        json.PropertyValueFor<string>(TargetProperty.FalseResult).Should().Be(Value.NoContents, "because the condition evaluated to be false");
    }

    [Fact]
    public void PipedMethod_IsSuccessful_WithCallToSingleAndMultiParameterMethod()
    {
        var json = ExecuteTestFor(_pipedMethods, _pipedMethodsDocument);

        json.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

        json.PropertyValueFor<bool>(TargetProperty.TrueResult).Should().BeTrue("because the property exists");
        json.PropertyValueFor<bool>(TargetProperty.FalseResult).Should().BeFalse("because the condition evaluated to be false");
        json.PropertyValueFor<int>(TargetProperty.IntegerLiteral).Should().Be(1, "because the decimal value should truncate during conversion");
        json.PropertyValueFor<string>(TargetProperty.StringLiteral).Should().Be("1.123", "because the decimal value should convert to string");
        json.PropertyValueFor<bool>(TargetProperty.BooleanLiteral).Should().Be(true, "because the string value should convert to boolean");
        json.PropertyValueFor<double>(TargetProperty.DoubleLiteral).Should().Be(1.123d, "because the decimal value should be preserved");
        json.PropertyValueFor<int>(TargetProperty.Length).Should().Be(5, "because that's the number of characters in the source string");
        json.PropertyValueFor<bool>(TargetProperty.StringContains).Should().BeTrue("because the string contains the appropriate value");
        json.PropertyValueFor<double>(TargetProperty.RoundedValue).Should().Be(1.12d, "because the double is supposed to be rounded to 2 places");
        json.PropertyValueFor<int>(TargetProperty.Sum).Should().Be(6, "because the array values add up to that number");
        json.PropertyValueFor<string>(TargetProperty.StringJoin).Should().Be("one,two,three", "because the array values should be joined by commas");
        json.PropertyValueFor<string>(TargetProperty.IntegerJoin).Should().Be("1,2,3", "because the array values should be joined by commas");
        json.PropertyValueFor<double>(TargetProperty.Average).Should().Be(2d, "because the array values should be averaged");
        json.PropertyValueFor<double>(TargetProperty.Min).Should().Be(1, "because that's the lowest value in the array");
        json.PropertyValueFor<double>(TargetProperty.Max).Should().Be(3, "because that's the highest value in the array");
        json.PropertyValueFor<bool>(TargetProperty.IsInteger).Should().Be(true, "because that's the type of the value");
        json.PropertyValueFor<bool>(TargetProperty.IsString).Should().Be(true, "because that's the type of the value");
        json.PropertyValueFor<bool>(TargetProperty.IsDouble).Should().Be(true, "because that's the type of the value");
        json.PropertyValueFor<bool>(TargetProperty.IsBoolean).Should().Be(true, "because that's the type of the value");
        json.PropertyValueFor<bool>(TargetProperty.IsArray).Should().Be(true, "because that's the type of the value");
        json.PropertyValueFor<int>(TargetProperty.Index).Should().Be(2, "because that's the first index of the value in the string");

        var array = (JArray)json[TargetProperty.Array];

        array.Should().NotBeNull("because a valid array created by splitting text should have been created");
        array.Count.Should().Be(3, "because there were two delimiters in the string which would leave three array entries");
        array[0].Value<int>().Should().Be(1, "because that is the first element value in the source document");
        array[1].Value<int>().Should().Be(2, "because that is the second element value in the source document");
        array[2].Value<int>().Should().Be(3, "because that is the third element value in the source document");

        json.PropertyValueFor<string>(TargetProperty.AppendedString).Should().Be("1.1231,2,3", "because the strings should be concatenated together");

        var appendedArray = (JArray)json[TargetProperty.AppendedArray];

        appendedArray.Should().NotBeNull("because both arrays exist in the source document");
        appendedArray.Count.Should().Be(6, "because there are three elements in each array");
        appendedArray[0].Value<string>().Should().Be("one", "because that is the first element");
        appendedArray[1].Value<string>().Should().Be("two", "because that is the second element");
        appendedArray[2].Value<string>().Should().Be("three", "because that is the third element");
        appendedArray[3].Value<string>().Should().Be("one", "because that is the first element");
        appendedArray[4].Value<string>().Should().Be("two", "because that is the second element");
        appendedArray[5].Value<string>().Should().Be("three", "because that is the third element");

        var appendedObject = (JObject)json[TargetProperty.AppendedObject];

        appendedObject.Should().NotBeNull("because both objects exist in the source document");
        appendedObject["first"].Value<int>().Should().Be(1, "because that is the value of the first property in the first object");
        appendedObject["second"].Value<int>().Should().Be(2, "because that is the value of the second property in the second object");
    }

    [Fact]
    public void ExternalMethod_IsSuccessful_WithStaticAndInstanceCallToSingleAndMultiParameterMethod()
    {
        var staticBoolRegistration = MethodRegistration.FromStaticMethod(typeof(ExternalStaticMethods), nameof(ExternalStaticMethods.TakesAndReturnsBoolean));
        var staticConcatRegistration = MethodRegistration.FromStaticMethod(typeof(ExternalStaticMethods), nameof(ExternalStaticMethods.Concatenate), "ConcatAlias");

        var instanceAppendRegistration = MethodRegistration.FromInstanceMethod(nameof(ExternalInstanceMethods.AppendString));

        var externalMethods = new[] { staticBoolRegistration, staticConcatRegistration, instanceAppendRegistration };

        var json = ExecuteTestFor(_externalMethods, _externalMethodsDocument, externalMethods, new ExternalInstanceMethods());

        json.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

        json.PropertyValueFor<bool>(TargetProperty.BooleanLiteral).Should().BeTrue("because that is the value in the source document");
        json.PropertyValueFor<string>(TargetProperty.StringLiteral).Should().Be("testtest", "because that is the value concatenated with itself in the source document");
        json.PropertyValueFor<string>(TargetProperty.AppendedString).Should().Be("testtest", "because that is the value appended twice with itself in the source document");
    }

    private JObject ExecuteTestFor(string transformerJson, string documentJson, IEnumerable<MethodRegistration> methodRegistrations = default, object? methodContext = default) => ExecuteTestFor(transformerJson, documentJson, JObject.Parse, methodRegistrations, methodContext);

    private void ValidateLiteralIsTransformed<T>(string transformerJson, string documentJson, string targetProperty, T targetValue)
    {
        var json = ExecuteTestFor(transformerJson, documentJson, JObject.Parse);

        json.PropertyValueFor<T>(targetProperty).Should().Be(targetValue, "because that was the value in the source document");
    }
}
