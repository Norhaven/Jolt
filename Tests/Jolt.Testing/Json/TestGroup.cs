using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace Jolt.Testing.Json
{
    public sealed class TestGroup
    {
        public string Name { get; set; }
        public IJsonObject Source { get; set; }
        public string ExternalMethodSource { get; set; }
        public EndToEndTest[] Tests { get; set; }
    }
}
