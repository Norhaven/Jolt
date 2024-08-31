using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Extensions
{
    internal static class SequenceExtensions
    {
        public static bool IsAnyOf<T>(this T currentValue, params T[] values)
        {
            foreach(var value in values ?? Enumerable.Empty<T>())
            {
                if (EqualityComparer<T>.Default.Equals(value, currentValue))
                {
                    return true;
                }
            }

            return false;
        }

        public static int? IndexOf<T>(this IEnumerable<T> sequence, Func<T, bool> isMatch)
        {
            var index = 0;

            foreach (var value in sequence)
            {
                if (isMatch(value))
                {
                    return index;
                }

                index++;
            }

            return default;
        }
    }
}
