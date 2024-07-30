using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Extensions
{
    internal static class RangeExtensions
    {
        public static int GetIndexIn(this Index index, string text) => index.IsFromEnd ? text.Length - index.Value - 1 : index.Value;
        public static int GetLengthIn(this Index index, Index end, string text) => end.GetIndexIn(text) - index.GetIndexIn(text);
        public static string Substring(this string text, Range range)
        {
            var startIndex = range.Start.GetIndexIn(text);
            var endIndex = range.End.GetIndexIn(text);

            if (startIndex > endIndex)
            {
                throw new ArgumentException($"Unable to perform a substring when the end index of '{endIndex}' comes before the start index of '{startIndex}'");
            }

            var length = endIndex - startIndex + 1;

            return text.Substring(startIndex, length);
        }
    }
}
