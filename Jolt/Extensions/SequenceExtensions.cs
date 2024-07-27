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
    }
}
