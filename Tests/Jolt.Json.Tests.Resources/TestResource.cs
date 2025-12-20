using System;
using System.IO;
using System.Reflection;

namespace Jolt.Json.Tests.Resources
{
    public static class TestResource
    {
        public static string GetByName(string resourceName)
        {
            var assembly = typeof(TestResource).Assembly;

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new ArgumentException($"Test resource '{resourceName}' not found.");
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static string ReadDocument(string fileName) => ReadTestFile($"Documents.{fileName}");
        public static string ReadTransformer(string fileName) => ReadTestFile($"Transformers.{fileName}");

        public static string ReadTestFile(string fileName) => GetByName($"Jolt.Json.Tests.Resources.TestFiles.{fileName}.json");
    }
}
