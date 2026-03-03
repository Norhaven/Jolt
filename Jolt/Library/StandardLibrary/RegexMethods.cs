using Jolt.Evaluation;
using Jolt.Extensions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Jolt.Library.StandardLibrary
{
    [IncludeInStandardLibrary]
    internal static class RegexMethods
    {
        [JoltLibraryMethod("isRegexMatch")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? IsRegexMatch(object? value, string pattern, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<string>(value);

            var text = resolved switch
            {
                string str => str,
                IJsonValue token when token.AsValue().ValueType == JsonValueType.String => token.AsValue().ToTypeOf<string>(),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to perform regex match for unsupported object type '{value?.GetType()}'")
            };

            var isMatch = Regex.IsMatch(text, pattern);

            return context.CreateTokenFrom(isMatch);
        }

        [JoltLibraryMethod("regexReplace")]
        [MethodIsValidOn(LibraryMethodTarget.PropertyValue)]
        public static IJsonToken? RegexReplace(object? value, string pattern, string replacement, EvaluationContext context)
        {
            var resolved = context.ResolveValueOf<string>(value);

            var text = resolved switch
            {
                string str => str,
                IJsonValue token when token.AsValue().ValueType == JsonValueType.String => token.AsValue().ToTypeOf<string>(),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unable to perform regex replace for unsupported object type '{value?.GetType()}'")
            };

            var replaced = Regex.Replace(text, pattern, replacement);

            return context.CreateTokenFrom(replaced);
        }
    }
}
