using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Json.Tests.Resources.TestAttributes
{
    public sealed class TestDefinitionAttribute : Attribute
    {
        public string TransformerName { get; }
        public string SourceDocumentName { get; }
        public Type ExternalMethodType { get; }

        public TestDefinitionAttribute(string transformerName, string sourceDocumentName, Type externalMethodType = default)
        {
            TransformerName = transformerName;
            SourceDocumentName = sourceDocumentName;
            ExternalMethodType = externalMethodType;
        }
    }
}
