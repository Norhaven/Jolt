using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Extensions
{
    internal static class RangeExtensions
    {
        public static int GetIndexIn(this Index index, string text) => index.IsFromEnd ? text.Length - index.Value : index.Value;

        public static string Substring(this string text, Range range)
        {
            var startIndex = range.Start.GetIndexIn(text);
            var endIndex = range.End.GetIndexIn(text);

            if (startIndex > endIndex)
            {
                throw new ArgumentException($"Unable to perform a substring when the end index of '{endIndex}' comes before the start index of '{startIndex}'");
            }

            var length = endIndex - startIndex;

            return text.Substring(startIndex, length);
        }
    }
}
