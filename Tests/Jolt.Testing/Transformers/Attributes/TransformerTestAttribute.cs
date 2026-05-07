using Jolt.Testing.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Sdk;

namespace Jolt.Testing.Transformers.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("Jolt.Testing.Transformers.TransformerTestDiscoverer", "Jolt.Testing")]
    public sealed class TransformerTestAttribute : TestDiscoveryAttribute
    {
        public string TransformerDocument { get; }
        public string SourceDocument { get; }
        public Type ExternalMethodsType { get; }

        public TransformerTestAttribute(string transformerDocument, string sourceDocument, Type testContextType, TestType testType, Type externalMethodsType = default) 
            : base(testContextType, testType)
        {
            TransformerDocument = transformerDocument;
            SourceDocument = sourceDocument;
            ExternalMethodsType = externalMethodsType;
        }
    }
}
