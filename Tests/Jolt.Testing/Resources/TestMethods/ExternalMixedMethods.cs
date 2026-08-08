using Jolt.Library;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Testing.Resources.TestMethods
{
    public class ExternalMixedMethods
    {
        public sealed class ComplexObject
        {
            public string Name { get; set; }
            public int Id { get; set; }
            public string Value { get; set; }
            public long AccumulatedValue { get; set; }
        }

        private readonly StringBuilder _builder = new StringBuilder();

        [JoltExternalMethod("aliasedAppend")]
        public string AppendWithAlias(string text)
        {
            _builder.Append(text);
            return _builder.ToString();
        }

        [JoltExternalMethod("booleanPassthrough")]
        public static bool PassBooleanThroughAndReturn(bool value) => value;

        [JoltExternalMethod("stringConcat")]
        public static string StringConcatenation(string first, string second) => first + second;

        [JoltExternalMethod("customStringFilterWithLambda")]
        public static IEnumerable<string> CustomStringFilterWithLambda(IEnumerable<string> sequence, Func<string, bool> filter)
        {
            foreach (var item in sequence)
            {
                if (filter(item))
                {
                    yield return item;
                }
            }
        }

        [JoltExternalMethod("customDoubleFilterWithLambda")]
        public static IEnumerable<double> CustomDoubleFilterWithLambda(IEnumerable<double> sequence, Func<double, bool> filter)
        {
            foreach (var item in sequence)
            {
                if (filter(item))
                {
                    yield return item;
                }
            }
        }

        [JoltExternalMethod("customLongFilterWithLambda")]
        public static IEnumerable<long> CustomLongFilterWithLambda(IEnumerable<long> sequence, Func<long, bool> filter)
        {
            foreach (var item in sequence)
            {
                if (filter(item))
                {
                    yield return item;
                }
            }
        }

        [JoltExternalMethod("customReduceWithLambda")]
        public static long CustomReduceWithLambda(IEnumerable<long> sequence, Func<long, long, long> reducer, long seed)
        {
            var result = seed;

            foreach (var item in sequence)
            {
                result = reducer(result, item);
            }

            return result;
        }

        [JoltExternalMethod("customBoolFilterWithLambda")]
        public static IEnumerable<string> CustomBoolFilterWithLambda(IEnumerable<bool> sequence, Func<bool, string> filter)
        {
            foreach (var item in sequence)
            {
                yield return filter(item);
            }
        }

        [JoltExternalMethod("customJsonObjectFilterWithLambda")]
        public static IEnumerable<string> CustomJsonObjectFilterWithLambda(IEnumerable<IJsonObject> sequence, Func<IJsonObject, string> filter)
        {
            foreach (var item in sequence)
            {
                yield return filter(item);
            }
        }

        [JoltExternalMethod("customStringToLongFilterWithLambda")]
        public static IEnumerable<long> CustomStringToLongFilterWithLambda(IEnumerable<string> sequence, Func<string, long> convertStringToLong)
        {
            foreach (var value in sequence)
            {
                var result = convertStringToLong(value);

                if (result <= 5)
                {
                    continue;
                }

                yield return result;
            }
        }

        [JoltExternalMethod("customTypeToLongFilterWithLambda")]
        public static IEnumerable<long> CustomTypeToLongFilterWithLambda(IEnumerable<string> sequence, Func<ComplexObject, long> convertTypeToLong)
        {
            foreach (var value in sequence)
            {
                var result = convertTypeToLong(new ComplexObject { Value = value });

                if (result <= 5)
                {
                    continue;
                }

                yield return result;
            }
        }

        [JoltExternalMethod("customTypeAccumulationWithLambda")]
        public static IEnumerable<ComplexObject> CustomTypeAccumulationWithLambda(IEnumerable<string> sequence, Func<ComplexObject, string, long> accumulate)
        {
            var currentResult = new ComplexObject { Name = "Accumulator", Id = 0, AccumulatedValue = 0 };

            foreach (var value in sequence)
            {
                currentResult.AccumulatedValue = accumulate(currentResult, value);
                currentResult.Id++;

                yield return currentResult;
            }
        }

        [JoltExternalMethod("stringPassthrough")]
        public static string PassStringThroughAndReturn(string value) => value;

        [JoltExternalMethod("integerPassthrough")]
        public static long PassLongThroughAndReturn(long value) => value;

        [JoltExternalMethod("doublePassthrough")]
        public static double PassDoubleThroughAndReturn(double value) => value;

        [JoltExternalMethod("jsonObjectPassthrough")]
        public static IJsonObject PassJsonObjectThroughAndReturn(IJsonObject obj) => obj;

        [JoltExternalMethod("jsonArrayPassthrough")]
        public static IJsonArray PassJsonArrayThroughAndReturn(IJsonArray array) => array;

        [JoltExternalMethod("returnsComplexObject")]
        public static ComplexObject ReturnsComplexObject() => new ComplexObject { Name = "Test", Id = 123, Value = "N/A" };
    }
}
