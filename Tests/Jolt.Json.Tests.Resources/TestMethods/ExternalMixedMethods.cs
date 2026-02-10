using Jolt.Library;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Json.Tests.Resources.TestMethods
{
    public class ExternalMixedMethods
    {
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
    }
}
