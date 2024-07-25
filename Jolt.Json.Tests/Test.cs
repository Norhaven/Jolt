using FluentAssertions;
using Jolt.Evaluation;
using Jolt.Library;
using Jolt.Parsing;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

        public const double DoubleLiteral = 1.123d;

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
    }

    protected static class TargetProperty
    {
        public const string IntegerLiteral = "Integer";
        public const string StringLiteral = "String";
        public const string DoubleLiteral = "Double";
        public const string BooleanTrueLiteral = "BooleanTrue";
        public const string BooleanFalseLiteral = "BooleanFalse";
        public const string Object = "Object";

        public const string ArrayElementId = "ArrayElementId";
        public const string Array = "Array";

        public const string GlobalId = "GlobalId";

        public const string Equation = "Equation";
        public const string LiteralEquation = "LiteralEquation";
        public const string Eval = "Eval";
        public const string Empty = "Empty";
        public const string TrueResult = "TrueResult";
        public const string FalseResult = "FalseResult";
        public const string Length = "Length";
        public const string StringContains = "StringContains";
        public const string RoundedValue = "RoundedValue";
        public const string Sum = "Sum";
        public const string StringJoin = "StringJoin";
        public const string IntegerJoin = "IntegerJoin";
        public const string GroupedEquation = "GroupedEquation";
    }

    protected readonly string _singleLevelDocument;
    protected readonly string _multiLevelDocument;
    protected readonly string _singleLevelLoopDocument;
    protected readonly string _mathDocument;
    protected readonly string _existenceDocument;
    protected readonly string _conditionsDocument;
    protected readonly string _pipedMethodsDocument;

    protected readonly string _singleLevelValueOf;
    protected readonly string _multiLevelValueOf;
    protected readonly string _singleLevelLoop;
    protected readonly string _math;
    protected readonly string _existence;
    protected readonly string _conditions;
    protected readonly string _pipedMethods;

    public Test()
    {
        _singleLevelDocument = ReadTestDocument("SingleLevelDocument");
        _multiLevelDocument = ReadTestDocument("MultiLevelDocument");
        _singleLevelLoopDocument = ReadTestDocument("SingleLevelLoopDocument");
        _mathDocument = ReadTestDocument("MathDocument");
        _existenceDocument = ReadTestDocument("ExistenceDocument");
        _conditionsDocument = ReadTestDocument("ConditionsDocument");
        _pipedMethodsDocument = ReadTestDocument("PipedMethodsDocument");

        _singleLevelValueOf = ReadTestFile("SingleLevelValueOf");
        _multiLevelValueOf = ReadTestFile("MultiLevelValueOf");
        _singleLevelLoop = ReadTestFile("SingleLevelLoop");
        _math = ReadTestFile("Math");
        _existence = ReadTestFile("Existence");
        _conditions = ReadTestFile("Conditions");
        _pipedMethods = ReadTestFile("PipedMethods");
    }

    protected abstract IJsonTokenReader CreateTokenReader();
    protected abstract IJsonTransformer<IJsonContext> CreateTransformer(JoltContext context);
    protected abstract IQueryPathProvider CreatePathQueryProvider();

    protected T? ExecuteTestFor<T>(string transformerJson, string testDocumentJson, Func<string?, T?> convertResult)
    {
        var transformer = CreateTransformerWith(transformerJson);
        var transformedDocument = transformer.Transform(testDocumentJson);

        transformedDocument.Should().NotBeNull("because a valid document was sent in and used by a valid transformer");

        return convertResult(transformedDocument);
    }

    protected IJsonTransformer<IJsonContext> CreateTransformerWith(string transformerJson)
    {
        var tokenReader = CreateTokenReader();
        var pathQueryProvider = CreatePathQueryProvider();

        var context = new JoltContext(
            transformerJson,
            new ExpressionParser(),
            new ExpressionEvaluator(),
            new TokenReader(),
            tokenReader,
            pathQueryProvider,
            new ReferenceResolver()
        );

        return CreateTransformer(context);
    }

    protected string ReadTestDocument(string fileName) => ReadTestFile($"Documents.{fileName}");

    protected string ReadTestFile(string fileName)
    {
        using var manifestStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Jolt.Json.Tests.TestFiles.{fileName}.json");
        using var reader = new StreamReader(manifestStream);

        return reader.ReadToEnd();
    }
}
