using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Jolt.LanguageMetadata
{
    /// <summary>
    /// Reads the method descriptions and examples from the "Library Methods" table in the repository README.
    /// </summary>
    internal static class ReadmeMethodTable
    {
        private const string SectionHeading = "# Library Methods";

        public sealed class Entry
        {
            public string Name { get; set; } = string.Empty;

            public string Description { get; set; } = string.Empty;

            public string Example { get; set; } = string.Empty;
        }

        public static Dictionary<string, Entry> ReadFrom(string readmePath)
        {
            var entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
            var isInSection = false;

            foreach (var rawLine in File.ReadLines(readmePath))
            {
                var line = rawLine.Trim();

                if (line == SectionHeading)
                {
                    isInSection = true;
                    continue;
                }

                if (!isInSection)
                {
                    continue;
                }

                if (line.StartsWith("# ", StringComparison.Ordinal))
                {
                    break;
                }

                if (!line.StartsWith("|", StringComparison.Ordinal))
                {
                    continue;
                }

                // Rows are "| name | description | example | valid on". The example column may itself contain
                // pipe characters (e.g. the || operator), so it is everything between the second and last cells.
                var cells = line.Split('|').Skip(1).Select(x => x.Trim()).ToArray();

                if (cells.Length < 4 || cells[0] == "Method" || cells[0].StartsWith("-", StringComparison.Ordinal))
                {
                    continue;
                }

                var example = string.Join("|", cells.Skip(2).Take(cells.Length - 3)).Trim().Trim('`');

                entries[cells[0]] = new Entry
                {
                    Name = cells[0],
                    Description = cells[1],
                    Example = example
                };
            }

            return entries;
        }
    }
}
