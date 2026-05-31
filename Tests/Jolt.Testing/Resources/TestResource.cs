using System;
using System.IO;
using System.Reflection;

namespace Jolt.Testing.Resources
{
    public static class TestResource
    {
        public static string GetByName(string resourceName)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
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
        public static string ReadValidations(string fileName) => ReadTestFile($"Validations.{fileName}");

        public static string ReadTestFile(string fileName) => GetByName($"Jolt.Testing.Resources.TestFiles.{fileName}.json");
    }
}
