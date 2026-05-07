using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Xunit.Abstractions;

namespace Jolt.Testing.Json
{
    public sealed class TestFile
    {
        public Dictionary<string, string> PossibleExceptionCodes { get; set; }
        public Dictionary<string, string> PossibleExternalMethodSources { get; set; }
        public TestGroup[] TestGroups { get; set; }
    }
}
