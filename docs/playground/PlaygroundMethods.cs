using Jolt.Library;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jolt.Playground
{
    /// <summary>
    /// External methods available in the playground, so the examples on the "External methods" page can be tried.
    /// These are the methods from that page; keep the two in step.
    /// </summary>
    public sealed class PlaygroundMethods
    {
        [JoltExternalMethod]
        public static bool IsNotNull(string value) => value != null;

        [JoltExternalMethod]
        public string ReverseString(string value) => new string(value.Reverse().ToArray());

        [JoltExternalMethod("convertAllStringsToLongs")]
        public static IEnumerable<long> ConvertAllStringsToLongs(IEnumerable<string> sequence, Func<string, long> convertStringToLong)
        {
            foreach (var value in sequence)
            {
                yield return convertStringToLong(value);
            }
        }

        [JoltExternalMethod("accumulateWithCustomType")]
        public static IEnumerable<ComplexObject> AccumulateWithCustomType(IEnumerable<long> sequence, Func<ComplexObject, long, long> accumulate)
        {
            var complexObject = new ComplexObject { Id = 0, Value = 0 };

            foreach (var value in sequence)
            {
                complexObject.Id++;
                complexObject.Value = accumulate(complexObject, value);

                yield return complexObject;
            }
        }

        /// <summary>
        /// Descriptions shown in the playground's list of available external methods.
        /// </summary>
        public static IReadOnlyList<(string Name, string Signature)> Descriptions { get; } = new[]
        {
            ("IsNotNull", "#IsNotNull(value) → boolean"),
            ("ReverseString", "#ReverseString(value) → string"),
            ("convertAllStringsToLongs", "#convertAllStringsToLongs(strings, @x: long) → long[]"),
            ("accumulateWithCustomType", "#accumulateWithCustomType(longs, @acc;@value: long) → { id, value }[]"),
        };
    }

    public sealed class ComplexObject
    {
        public int Id { get; set; }

        public long Value { get; set; }
    }
}
