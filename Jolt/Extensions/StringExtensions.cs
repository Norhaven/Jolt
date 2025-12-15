using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Extensions
{
    public static class StringExtensions
    {
        public static string Join(this string[] values, char delimiter)
        {
#if NETSTANDARD2_0
            return string.Join(delimiter.ToString(), values);
#else
            return string.Join(delimiter, values);
#endif
        }

        public static string[] Split(this string value, string delimiter, StringSplitOptions options = StringSplitOptions.None)
        {
#if NETSTANDARD2_0
            return value.Split(new string[] { delimiter }, options);
#else
            return value.Split(delimiter, options);
#endif
        }

        public static bool Contains(this string value, char targetCharacter)
        {
#if NETSTANDARD2_0
            return value.Contains(targetCharacter.ToString());
#else
            return value.Contains(targetCharacter);
#endif
        }

        public static bool StartsWith(this string value, char targetCharacter)
        {
#if NETSTANDARD2_0
            return value.StartsWith(targetCharacter.ToString());
#else
            return value.StartsWith(targetCharacter);
#endif
        }
    }
}
