using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Json.Tests.Resources.TestAttributes
{
    public sealed class TransformerTestDefinitionAttribute : Attribute
    {
        public string TransformerName { get; }
        public string SourceDocumentName { get; }
        public Type ExternalMethodType { get; }

        public TransformerTestDefinitionAttribute(string transformerName, string sourceDocumentName, Type externalMethodType = default)
        {
            TransformerName = transformerName;
            SourceDocumentName = sourceDocumentName;
            ExternalMethodType = externalMethodType;
        }
    }
}
