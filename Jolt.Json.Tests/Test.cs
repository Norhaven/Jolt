using FluentAssertions;
using Jolt.Evaluation;
using Jolt.Json.Tests.TestAttributes;
using Jolt.Library;
using Jolt.Parsing;
using Jolt.Structure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft = Jolt.Json.Newtonsoft;
using DotNet = Jolt.Json.DotNet;
using Xunit.DependencyInjection;

namespace Jolt.Json.Tests;

public abstract class Test
{
    protected static class Value
    {
        public const int IntegerLiteral = 1;
        public const string StringLiteral = "test";
        public const string NestedStringLiteral = "nestedTest";
        public const bool BooleanTrueLiteral = true;
        public const bool BooleanFalseLiteral = false;

        public const int FirstArrayElementId = 1;
        public const int SecondArrayElementId = 2;
        public const int GlobalId = 3;

        public const double DecimalLiteral = 1.123d;

        public const string Contents = "contents";
        public const string HasContents = "Has Contents";
        public const string NoContents = "No Contents";
    }

    protected static class SourceProperty
    {
        public const string IntegerLiteral = "integerLiteral";
        public const string StringLiteral = "stringLiteral";
        public const string BooleanTrue = "booleanTrueLiteral";
        public const string BooleanFalse = "booleanFalseLiteral";
        public const string Object = "object";
        public const string MathExpression = "mathExpression";

        public const string ArrayElementId = "arrayElementId";
        public const string Value = "value";
    }

    protected static class TargetProperty
    {
        public const string IntegerLiteral = "Integer";
        public const string StringLiteral = "String";
        public const string DecimalLiteral = "Decimal";
        public const string BooleanLiteral = "Boolean";
        public const string BooleanTrueLiteral = "BooleanTrue";
        public const string BooleanFalseLiteral = "BooleanFalse";
        public const string Object = "Object";
        public const string Result = "Result";
        public const string ArrayElementId = "ArrayElementId";
        public const string Array = "Array";

        public const string GlobalId = "GlobalId";

        public const string Equation = "Equation";
        public const string LiteralEquation = "LiteralEquation";
        public const string Eval = "Eval";
        public const string TrueResult = "TrueResult";
        public const string FalseResult = "FalseResult";
        public const string Length = "Length";
        public const string StringContains = "StringContains";
        public const string RoundedValue = "RoundedValue";
        public const string Sum = "Sum";
        public const string Average = "Average";
        public const string Min = "Min";
        public const string Max = "Max";
        public const string Empty = "Empty";
        public const string Any = "Any";
        public const string StringJoin = "StringJoin";
        public const string IntegerJoin = "IntegerJoin";
        public const string GroupedEquation = "GroupedEquation";
        public const string IsInteger = "IsInteger";
        public const string IsString = "IsString";
        public const string IsDecimal = "IsDecimal";
        public const string IsBoolean = "IsBoolean";
        public const string IsArray = "IsArray";
        public const string Index = "Index";
        public const string AppendedString = "AppendedString";
        public const string AppendedArray = "AppendedArray";
        public const string AppendedObject = "AppendedObject";
        public const string AppendedVariadic = "AppendedVariadic";
        public const string ObjectFromArray = "ObjectFromArray";
        public const string ArrayFromObject = "ArrayFromObject";
        public const string Group = "Group";
        public const string Order = "Order";
        public const string Order1 = "Order1";
        public const string OrderDesc = "OrderDesc";
        public const string OrderDesc1 = "OrderDesc1";
        public const string Substring1 = "Substring1";
        public const string Substring2 = "Substring2";
        public const string Substring3 = "Substring3";
        public const string Substring4 = "Substring4";
        public const string Substring5 = "Substring5";
        public const string Substring6 = "Substring6";

        public const string First = "first";
        public const string Second = "second";
    }

    protected readonly string _singleLevelDocument;
    protected readonly string _multiLevelDocument;
    protected readonly string _singleLevelLoopDocument;
    protected readonly string _mathDocument;
    protected readonly string _existenceDocument;
    protected readonly string _conditionsDocument;
    protected readonly string _pipedMethodsDocument;
    protected readonly string _externalMethodsDocument;
    protected readonly string _lambdasDocument;

    protected readonly string _singleLevelValueOf;
    protected readonly string _multiLevelValueOf;
    protected readonly string _singleLevelLoop;
    protected readonly string _math;
    protected readonly string _existence;
    protected readonly string _conditions;
    protected readonly string _pipedMethods;
    protected readonly string _externalMethods;
    protected readonly string _rangeVariables;
    protected readonly string _lambdas;

    protected readonly IJsonContext _testContext;
    protected readonly IJsonTransformer<IJsonContext> _transformer;

    public Test(IJsonContext context)
    {
        _singleLevelDocument = ReadTestDocument("SingleLevelDocument");
        _multiLevelDocument = ReadTestDocument("MultiLevelDocument");
        _singleLevelLoopDocument = ReadTestDocument("SingleLevelLoopDocument");
        _mathDocument = ReadTestDocument("MathDocument");
        _existenceDocument = ReadTestDocument("ExistenceDocument");
        _conditionsDocument = ReadTestDocument("ConditionsDocument");
        _pipedMethodsDocument = ReadTestDocument("PipedMethodsDocument");
        _externalMethodsDocument = ReadTestDocument("ExternalMethodsDocument");
        _lambdasDocument = ReadTestDocument("LambdasDocument");

        _singleLevelValueOf = ReadTestTransformer("SingleLevelValueOf");
        _multiLevelValueOf = ReadTestTransformer("MultiLevelValueOf");
        _singleLevelLoop = ReadTestTransformer("SingleLevelLoop");
        _math = ReadTestTransformer("Math");
        _existence = ReadTestTransformer("Existence");
        _conditions = ReadTestTransformer("Conditions");
        _pipedMethods = ReadTestTransformer("PipedMethods");
        _externalMethods = ReadTestTransformer("ExternalMethods");
        _rangeVariables = ReadTestTransformer("RangeVariables");
        _lambdas = ReadTestTransformer("Lambdas");

        _testContext = context;
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            IJsonContext CreateNewtonsoftContext()
            {
                return new JoltContext(
                    default,
                    new ExpressionParser(),
                    new ExpressionEvaluator(),
                    new TokenReader(),
                    new Newtonsoft.JsonTokenReader(),
                    new Newtonsoft.JsonPathQueryPathProvider(),
                    new MethodReferenceResolver()
                );
            }

            IJsonContext CreateDotNetContext()
            {
                return new JoltContext(
                    default,
                    new ExpressionParser(),
                    new ExpressionEvaluator(),
                    new TokenReader(),
                    new DotNet.JsonTokenReader(),
                    new DotNet.JsonPathQueryPathProvider(),
                    new MethodReferenceResolver()
                );
            }

            services
                .AddKeyedTransient(TestType.Newtonsoft, (x, _) => CreateNewtonsoftContext())
                .AddKeyedTransient(TestType.DotNet, (x, _) => CreateDotNetContext());            
        }
    }

    protected IJsonObject? ExecuteTestFor(string transformerJson, string testDocumentJson, IEnumerable<MethodRegistration> methodRegistrations = default, object? methodContext = default)
    {
        var transformer = CreateTransformerWith(transformerJson, methodRegistrations, methodContext);
        var transformedDocument = transformer.Transform(testDocumentJson);

        transformedDocument.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

        return _testContext.JsonTokenReader.Read(transformedDocument) as IJsonObject;
    }

    protected IJsonTransformer<IJsonContext> CreateTransformerWith(string transformerJson, IEnumerable<MethodRegistration> methodRegistrations, object? methodContext = default)
    {
        var context = _testContext
            .UseTransformer(transformerJson)
            .RegisterAllMethods(methodRegistrations)
            .UseMethodContext(methodContext);

        return new JoltTransformer<IJsonContext>(context);
    }

    protected string ReadTestDocument(string fileName) => ReadTestFile($"Documents.{fileName}");
    protected string ReadTestTransformer(string fileName) => ReadTestFile($"Transformers.{fileName}");

    protected string ReadTestFile(string fileName)
    {
        using var manifestStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Jolt.Json.Tests.TestFiles.{fileName}.json");
        using var reader = new StreamReader(manifestStream);

        return reader.ReadToEnd();
    }
}
