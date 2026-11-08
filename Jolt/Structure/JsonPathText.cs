using Jolt.Extensions;
using System.Linq;
using System.Text;

namespace Jolt.Structure
{
    /// <summary>
    /// Helps with JSON path text for the RFC 9535 parser used by the JSON providers.
    /// </summary>
    public static class JsonPathText
    {
        private const string CharactersNotAllowedInShorthandName = "'\"\\()[]{},*?+/=!<>&|";

        /// <summary>
        /// Finds the first dotted name that RFC 9535 doesn't allow after a dot (e.g. the "first-name" in "$.first-name"), along with the
        /// path written using bracket notation for it instead (e.g. "$['first-name']"), so that the error for it can say how to fix it.
        /// </summary>
        public static bool TryFindNameRequiringBracketNotation(string path, out string name, out string suggestedPath)
        {
            suggestedPath = WithBracketNotation(path, out var firstName);
            name = firstName ?? string.Empty;

            return firstName != null;
        }

        /// <summary>
        /// Rewrites any dotted name that RFC 9535 doesn't allow in its shorthand form into bracket notation (e.g. "$.first-name" becomes "$['first-name']").
        /// Anything within square brackets stays as it is, as does a name containing a character that needs to be quoted, which is left for the parser to reject.
        /// </summary>
        private static string WithBracketNotation(string path, out string? firstRewrittenName)
        {
            firstRewrittenName = default;

            if (string.IsNullOrEmpty(path) || path[0] != '$')
            {
                return path;
            }

            var result = new StringBuilder(path.Length + 4);
            var position = 1;

            result.Append('$');

            while (position < path.Length)
            {
                if (path[position] == '[')
                {
                    var end = FindEndOfBracket(path, position);

                    result.Append(path, position, end - position);
                    position = end;
                }
                else if (path[position] == '.')
                {
                    var isRecursive = position + 1 < path.Length && path[position + 1] == '.';
                    var start = position + (isRecursive ? 2 : 1);
                    var end = start;

                    while (end < path.Length && path[end] != '.' && path[end] != '[')
                    {
                        end++;
                    }

                    var name = path.Substring(start, end - start);

                    if (RequiresBracketNotation(name))
                    {
                        firstRewrittenName ??= name;

                        result.Append(isRecursive ? ".." : string.Empty).Append("['").Append(name).Append("']");
                    }
                    else
                    {
                        result.Append(path, position, end - position);
                    }

                    position = end;
                }
                else
                {
                    result.Append(path[position++]);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Creates the error for a path that can't be parsed, which points to bracket notation for any dotted name that needs it.
        /// </summary>
        internal static Exceptions.JoltException CreateParsingErrorFor<T>(IJsonContext context, string path)
        {
            if (TryFindNameRequiringBracketNotation(path, out var name, out var suggestedPath))
            {
                var subtractionHint = name.Contains("-") ? ", or spaces around the '-' if it's a subtraction" : string.Empty;

                return context.CreateParsingErrorFor<T>(Exceptions.ExceptionCode.UnableToUseJsonPathNameWithoutBracketNotation, path, name, suggestedPath, subtractionHint);
            }

            return context.CreateParsingErrorFor<T>(Exceptions.ExceptionCode.UnableToParseJsonPath, path);
        }

        /// <summary>
        /// Prepares a path that may be relative to the token it's selecting from (e.g. "a.b"), as well as one that starts at "$". A relative
        /// path is put together from property names (e.g. by #setAt), so any name that needs bracket notation is given it, while a path that
        /// starts at "$" was written by the user and is passed on as it is.
        /// </summary>
        public static string NormalizeRelative(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "$";
            }

            return path[0] switch
            {
                '$' => path,
                '[' => WithBracketNotation("$" + path, out _),
                _ => WithBracketNotation("$." + path, out _)
            };
        }

        private static bool RequiresBracketNotation(string name)
        {
            return name.Length > 0 && !IsValidShorthandName(name) && !name.Any(x => char.IsWhiteSpace(x) || CharactersNotAllowedInShorthandName.IndexOf(x) >= 0);
        }

        private static bool IsValidShorthandName(string name)
        {
            return !char.IsDigit(name[0]) && name.All(x => x == '_' || char.IsLetterOrDigit(x));
        }

        private static int FindEndOfBracket(string path, int start)
        {
            var nesting = 0;
            var quote = default(char?);

            for (var i = start; i < path.Length; i++)
            {
                var current = path[i];

                if (quote != null)
                {
                    if (current == '\\')
                    {
                        i++;
                    }
                    else if (current == quote)
                    {
                        quote = default;
                    }
                }
                else if (current == '\'' || current == '"')
                {
                    quote = current;
                }
                else if (current == '[')
                {
                    nesting++;
                }
                else if (current == ']' && --nesting == 0)
                {
                    return i + 1;
                }
            }

            return path.Length;
        }
    }
}
