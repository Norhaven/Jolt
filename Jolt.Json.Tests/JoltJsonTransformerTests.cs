using FluentAssertions;
using Jolt.Json.Newtonsoft;
using Jolt.Json.Tests.Extensions;
using Jolt.Json.Tests.TestAttributes;
using Jolt.Json.Tests.TestMethods;
using Jolt.Library;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests;

public abstract class JoltJsonTransformerTests : Test
{    
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

        var nestedJson = (IJsonObject)json[TargetProperty.Object];

        nestedJson.PropertyValueFor<string>(TargetProperty.StringLiteral).Should().Be(Value.StringLiteral, "because that was the value in the source document");
    }

    [Fact]
    public void Loop_IsSuccessful_AtSingleLevelForMultiElements()
    {
        var json = ExecuteTestFor(_singleLevelLoop, _singleLevelLoopDocument);

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
        arrayFromObject[1].AsObject()[TargetProperty.Second].ToString().Should().Be("2", "because that is the value of the first property in the source object");

        var objectFromArray = (IJsonObject)json[TargetProperty.ObjectFromArray];

        objectFromArray.Should().NotBeNull("because an object should have been created due to the existence of the source array with elements");

        objectFromArray.PropertyValueFor<int>(TargetProperty.Result).Should().Be(2, "because the last element of the array contained that value in its property");
    }

    [Fact]
    public void ValueOf_WorksSuccessfullyAtMultipleLevels()
    {
        var json = ExecuteTestFor(_multiLevelValueOf, _multiLevelDocument);

        json.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

        var objectProperty = (IJsonObject)json[TargetProperty.Object];

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
        json.PropertyValueFor<bool>(TargetProperty.BooleanTrueLiteral).Should().Be(true, "because this is the result of evaluating the equation");
        json.PropertyValueFor<bool>(TargetProperty.BooleanLiteral).Should().Be(true, "because this is the result of evaluating the equation");
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

        var firstObject = (IJsonObject)json[TargetProperty.Object];

        firstObject.Should().NotBeNull("because the condition should have evaluated to true");
        firstObject.PropertyValueFor<string>(TargetProperty.First).Should().Be(Value.Contents, "because its expression read that value");

        var secondObject = (IJsonValue)json[TargetProperty.Result];

        if (secondObject is not null)
        {
            secondObject.ToTypeOf<object>().Should().BeNull("because the condition should have evaluated to false");
        }
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

    [Fact]
    [SourceHas(SourceValueType.StringLiteral, SourceProperty.StringLiteral, "test")]
    [TransformerIs(TargetProperty.StringLiteral, $"#valueOf($.{SourceProperty.StringLiteral})")]
    [ExpectsResult(TargetProperty.StringLiteral, "test")]
    public void ValueOf_IsSuccessful_WithStringLiteral() => ExecuteSmallTest();

    [Fact]
    [SourceHas(SourceValueType.IntegerLiteral, SourceProperty.IntegerLiteral, 1)]
    [TransformerIs(TargetProperty.IntegerLiteral, $"#valueOf($.{SourceProperty.IntegerLiteral})")]
    [ExpectsResult(TargetProperty.IntegerLiteral, 1)]
    public void ValueOf_IsSuccessful_WithIntegerLiteral() => ExecuteSmallTest();

    protected void ExecuteSmallTest([CallerMemberName] string methodName = default)
    {
        var method = GetType().GetMethod(methodName);
        var source = method.GetCustomAttribute<SourceHasAttribute>();
        var target = method.GetCustomAttribute<TransformerIsAttribute>();
        var expectsResult = method.GetCustomAttribute<ExpectsResultAttribute>();
        var expectsException = method.GetCustomAttribute<ExpectsExceptionAttribute>();

        if (method is null)
        {
            throw new ArgumentNullException(nameof(method), $"Unable to locate test method '{methodName}'");
        }

        if (source is null || target is null)
        {
            throw new ArgumentNullException(nameof(source), $"Either source or transformer is missing for test method '{methodName}'");
        }

        var reader = CreateTokenReader();

        var sourceJson = reader.Read("{}") as IJsonObject;
        var transformerJson = reader.Read("{}") as IJsonObject;

        sourceJson[source.Name] = reader.CreateTokenFrom(source.Value);
        transformerJson[target.NameExpression] = reader.CreateTokenFrom(target.ValueExpression);

        var transformer = CreateTransformerWith(transformerJson.ToString(), []);

        try
        {
            var result = transformer.Transform(sourceJson.ToString());

            var jsonResult = reader.Read(result) as IJsonObject;            
            jsonResult[expectsResult.PropertyName].ToTypeOf<object>().Should().Be(expectsResult.Value, "because the result was expected by the test");
        }
        catch(Exception ex)
        {
            if (expectsException is null)
            {
                throw;
            }

            expectsException.ExceptionType.Should().Be(ex.GetType(), "because this exception was expected");
        }
    }

    private IJsonObject ExecuteTestFor(string transformerJson, string documentJson, IEnumerable<MethodRegistration> methodRegistrations = default, object? methodContext = default) => ExecuteTestFor(transformerJson, documentJson, ParseJson, methodRegistrations, methodContext);

    private void ValidateLiteralIsTransformed<T>(string transformerJson, string documentJson, string targetProperty, T targetValue)
    {
        var json = ExecuteTestFor(transformerJson, documentJson, ParseJson);

        json.PropertyValueFor<T>(targetProperty).Should().Be(targetValue, "because that was the value in the source document");
    }
}
