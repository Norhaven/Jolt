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
using Jolt.Exceptions;

namespace Jolt.Json.Tests;

public abstract class Test(IJsonContext context)
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
        public const string RootObject = "RootObject";
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

    protected readonly string _singleLevelDocument = ReadTestDocument("SingleLevelDocument");
    protected readonly string _multiLevelDocument = ReadTestDocument("MultiLevelDocument");
    protected readonly string _loopDocument = ReadTestDocument("LoopDocument");
    protected readonly string _mathDocument = ReadTestDocument("MathDocument");
    protected readonly string _existenceDocument = ReadTestDocument("ExistenceDocument");
    protected readonly string _conditionsDocument = ReadTestDocument("ConditionsDocument");
    protected readonly string _pipedMethodsDocument = ReadTestDocument("PipedMethodsDocument");
    protected readonly string _externalMethodsDocument = ReadTestDocument("ExternalMethodsDocument");
    protected readonly string _lambdasDocument = ReadTestDocument("LambdasDocument");

    protected readonly string _singleLevelValueOf = ReadTestTransformer("SingleLevelValueOf");
    protected readonly string _multiLevelValueOf = ReadTestTransformer("MultiLevelValueOf");
    protected readonly string _loops = ReadTestTransformer("Loops");
    protected readonly string _math = ReadTestTransformer("Math");
    protected readonly string _existence = ReadTestTransformer("Existence");
    protected readonly string _conditions = ReadTestTransformer("Conditions");
    protected readonly string _pipedMethods = ReadTestTransformer("PipedMethods");
    protected readonly string _externalMethods = ReadTestTransformer("ExternalMethods");
    protected readonly string _rangeVariables = ReadTestTransformer("RangeVariables");
    protected readonly string _lambdas = ReadTestTransformer("Lambdas");
    protected readonly string _usingBlock = ReadTestTransformer("UsingBlock");

    protected readonly IJsonContext _testContext = context;
    protected readonly IJsonTransformer<IJsonContext> _transformer;

    public class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            IJsonContext CreateNewtonsoftContext()
            {
                var messageProvider = new MessageProvider(JoltOptions.Default);

                return new JoltContext(
                    default,
                    new ExpressionParser(),
                    new ExpressionEvaluator(),
                    new TokenReader(messageProvider),
                    new Newtonsoft.JsonTokenReader(),
                    new Newtonsoft.JsonPathQueryPathProvider(),
                    new MethodReferenceResolver(messageProvider),
                    messageProvider,
                    new ErrorHandler(default)
                );
            }

            IJsonContext CreateDotNetContext()
            {
                var messageProvider = new MessageProvider(JoltOptions.Default);

                return new JoltContext(
                    default,
                    new ExpressionParser(),
                    new ExpressionEvaluator(),
                    new TokenReader(messageProvider),
                    new DotNet.JsonTokenReader(),
                    new DotNet.JsonPathQueryPathProvider(),
                    new MethodReferenceResolver(messageProvider),
                    messageProvider,
                    new ErrorHandler(default)
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

    protected static string ReadTestDocument(string fileName) => ReadTestFile($"Documents.{fileName}");
    protected static string ReadTestTransformer(string fileName) => ReadTestFile($"Transformers.{fileName}");

    protected static string ReadTestFile(string fileName)
    {
        using var manifestStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Jolt.Json.Tests.TestFiles.{fileName}.json");
        using var reader = new StreamReader(manifestStream);

        return reader.ReadToEnd();
    }
}
