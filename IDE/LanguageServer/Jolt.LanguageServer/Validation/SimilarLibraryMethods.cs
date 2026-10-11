using System;
using System.Linq;
using System.Reflection;

namespace Jolt.LanguageServer.Validation
{
    /// <summary>
    /// Finds the standard library method that an unknown method name was most likely meant to be, e.g. "valueOf" for
    /// "valeuOf" or "ValueOf", so that a misspelling can be reported even when the host's custom methods aren't known.
    /// </summary>
    internal static class SimilarLibraryMethods
    {
        // Jolt doesn't list its standard library publicly, and its library attributes are internal, so methods are found
        // by the name of their attribute, as the language metadata tool does.
        private const string JoltLibraryMethodAttribute = "Jolt.Library.JoltLibraryMethodAttribute";

        private static readonly Lazy<string[]> _names = new Lazy<string[]>(ReadNames);

        /// <summary>
        /// Gets the closest standard library method name to the given name, or null when none is close enough. Names
        /// allow one difference (an inserted, deleted, changed, or swapped character) for every four characters.
        /// </summary>
        public static string? FindClosest(string name)
        {
            var maxDistance = Math.Max(1, name.Length / 4);
            var lowerName = name.ToLowerInvariant();

            return _names.Value
                .Where(x => x != name)
                .Select(x => (Name: x, Distance: GetEditDistance(lowerName, x.ToLowerInvariant())))
                .Where(x => x.Distance <= maxDistance)
                .OrderBy(x => x.Distance)
                .ThenBy(x => x.Name, StringComparer.Ordinal)
                .Select(x => x.Name)
                .FirstOrDefault();
        }

        private static string[] ReadNames()
        {
            var methods = typeof(IJsonContext).Assembly
                .GetTypes()
                .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                .Select(x => x.CustomAttributes.FirstOrDefault(y => y.AttributeType.FullName == JoltLibraryMethodAttribute))
                .Where(x => x != null)
                .Select(x => x!.ConstructorArguments);

            // Unsafe methods (e.g. #eval) aren't suggested, since they only work when the host application enables them.
            return methods
                .Where(x => !(x.Count > 2 && x[2].Value is true))
                .Select(x => (string)x[0].Value!)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Gets the fewest single character insertions, deletions, substitutions, or swaps of adjacent characters that
        /// turn one string into the other.
        /// </summary>
        private static int GetEditDistance(string name, string systemName)
        {
            var distances = new int[name.Length + 1, systemName.Length + 1];

            for (var i = 0; i <= name.Length; i++)
            {
                distances[i, 0] = i;
            }

            for (var j = 0; j <= systemName.Length; j++)
            {
                distances[0, j] = j;
            }

            for (var i = 1; i <= name.Length; i++)
            {
                for (var j = 1; j <= systemName.Length; j++)
                {
                    distances[i, j] = Math.Min(
                        Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                        distances[i - 1, j - 1] + (name[i - 1] == systemName[j - 1] ? 0 : 1));

                    if (i > 1 && j > 1 && name[i - 1] == systemName[j - 2] && name[i - 2] == systemName[j - 1])
                    {
                        distances[i, j] = Math.Min(distances[i, j], distances[i - 2, j - 2] + 1);
                    }
                }
            }

            return distances[name.Length, systemName.Length];
        }
    }
}
