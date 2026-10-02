using Jolt;
using System;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jolt.LanguageMetadata
{
    /// <summary>
    /// Generates the language metadata manifest (library methods, their signatures, and documentation) that IDE
    /// extensions use for completions, signature help, and hover information.
    /// </summary>
    /// <remarks>
    /// Usage: dotnet run --project IDE/Tools/Jolt.LanguageMetadata -- [--output path] [--readme path] [--check]
    /// With --check, the manifest is not written and the exit code is 1 if the existing file is out of date.
    /// </remarks>
    internal static class Program
    {
        private const string DefaultOutputPath = "IDE/Extensions/VSCode/jolt/src/data/library-methods.json";

        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            NewLine = "\n",
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static int Main(string[] args)
        {
            var repositoryRoot = FindRepositoryRoot();
            var outputPath = GetOption(args, "--output") ?? Path.Combine(repositoryRoot, DefaultOutputPath);
            var readmePath = GetOption(args, "--readme") ?? Path.Combine(repositoryRoot, "README.md");
            var isCheckOnly = args.Contains("--check");

            var assembly = typeof(IJsonContext).Assembly;
            var methods = LibraryMethodReader.ReadFrom(assembly);
            var readme = ReadmeMethodTable.ReadFrom(readmePath);

            foreach (var method in methods)
            {
                if (readme.TryGetValue(method.Name, out var entry))
                {
                    method.Description = entry.Description;
                    method.Example = entry.Example;
                }
                else
                {
                    Console.Error.WriteLine($"warning: library method '{method.Name}' is not documented in the README method table");
                }
            }

            foreach (var name in readme.Keys.Except(methods.Select(x => x.Name)))
            {
                Console.Error.WriteLine($"warning: README method table documents '{name}', which is not a library method");
            }

            var manifest = new LanguageManifest
            {
                JoltVersion = assembly.GetName().Version?.ToString(3) ?? string.Empty,
                Methods = methods
            };

            var json = JsonSerializer.Serialize(manifest, SerializerOptions) + "\n";

            if (isCheckOnly)
            {
                var existing = File.Exists(outputPath) ? File.ReadAllText(outputPath).Replace("\r\n", "\n") : string.Empty;

                if (existing != json)
                {
                    Console.Error.WriteLine($"error: {outputPath} is out of date, run this tool to regenerate it");
                    return 1;
                }

                Console.WriteLine($"{outputPath} is up to date ({methods.Count} methods)");
                return 0;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, json);

            Console.WriteLine($"Wrote {methods.Count} methods to {outputPath}");
            return 0;
        }

        private static string? GetOption(string[] args, string name)
        {
            var index = Array.IndexOf(args, name);

            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        private static string FindRepositoryRoot()
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                for (var directory = new DirectoryInfo(start); directory != null; directory = directory.Parent)
                {
                    if (File.Exists(Path.Combine(directory.FullName, "Jolt.sln")))
                    {
                        return directory.FullName;
                    }
                }
            }

            throw new InvalidOperationException("Unable to locate the repository root (the directory containing Jolt.sln)");
        }
    }
}
