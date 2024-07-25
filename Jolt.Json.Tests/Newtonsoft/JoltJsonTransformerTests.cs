using FluentAssertions;
using Jolt.Json.Newtonsoft;
using Jolt.Json.Tests.TestExtensions;
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
    protected override IQueryPathProvider CreatePathQueryProvider() => new JsonPathQueryPathProvider();

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
    public void PipedMethod_IsSuccessful_WithCallToSingleParameterMethod()
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

        var array = (JArray)json[TargetProperty.Array];

        array.Should().NotBeNull("because a valid array created by splitting text should have been created");
        array.Count.Should().Be(3, "because there were two delimiters in the string which would leave three array entries");
        array[0].Value<int>().Should().Be(1, "because that is the first element value in the source document");
        array[1].Value<int>().Should().Be(2, "because that is the second element value in the source document");
        array[2].Value<int>().Should().Be(3, "because that is the third element value in the source document");
    }

    private JObject ExecuteTestFor(string transformerJson, string documentJson) => ExecuteTestFor(transformerJson, documentJson, JObject.Parse);

    private void ValidateLiteralIsTransformed<T>(string transformerJson, string documentJson, string targetProperty, T targetValue)
    {
        var json = ExecuteTestFor(transformerJson, documentJson, JObject.Parse);

        json.PropertyValueFor<T>(targetProperty).Should().Be(targetValue, "because that was the value in the source document");
    }
}
