using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Json.Tests.Resources.TestAttributes;
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
using static Jolt.Json.Tests.Resources.SmallTest;

namespace Jolt.Json.Tests.Resources
{
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
            public const string RootObject = "RootObject";
            public const string Group = "Group";
            public const string Summary = "Summary";
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

        public static class SourceDocument
        {
            public const string SingleLevel = "SingleLevelDocument";
            public const string MultiLevel = "MultiLevelDocument";
            public const string Loop = "LoopDocument";
            public const string Math = "MathDocument";
            public const string Existence = "ExistenceDocument";
            public const string Conditions = "ConditionsDocument";
            public const string PipedMethods = "PipedMethodsDocument";
            public const string ExternalMethods = "ExternalMethodsDocument";
            public const string Lambdas = "LambdasDocument";
        }

        public static class Transformer
        {
            public const string SingleLevelValueOf = "SingleLevelValueOf";
            public const string MultiLevelValueOf = "MultiLevelValueOf";
            public const string Loops = "Loops";
            public const string Math = "Math";
            public const string Existence = "Existence";
            public const string Conditions = "Conditions";
            public const string PipedMethods = "PipedMethods";
            public const string ExternalMethods = "ExternalMethods";
            public const string ExternalMethodsWithAliases = "ExternalMethodsWithAliases";
            public const string RangeVariables = "RangeVariables";
            public const string Lambdas = "Lambdas";
            public const string UsingBlock = "UsingBlock";
        }

        public abstract class TestContainer
        {
            protected readonly MethodInfo _method;
            protected readonly Attribute _attribute;

            public string TestName => _method.Name;

            public TestContainer(MethodInfo testMethod, Attribute attribute)
            {
                _method = testMethod;
                _attribute = attribute;
            }

            public abstract void Execute(IJsonContext context);
        }

        protected readonly IJsonContext _testContext;

        public Test(IJsonContext context)
        {
            _testContext = context;
        }
                
        public static IEnumerable<object[]> GetAllTestsInScope(Type testClassType, Type testAttributeType, Type testContainerType)
        {
            var testMethods = from method in testClassType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                              let attribute = method.GetCustomAttribute(testAttributeType)
                              where attribute != null
                              select new { Method = method, Attribute = attribute };

            foreach (var testMethod in testMethods)
            {
                yield return new[] { Activator.CreateInstance(testContainerType, testMethod.Method, testMethod.Attribute) };
            }
        }
    }
}
