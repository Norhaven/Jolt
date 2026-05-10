using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Testing.Json
{
    public sealed class TestDefinition
    {
        public string Name { get; set; }
        public IJsonObject Transformer { get; set; }
        public IJsonObject Result { get; set; }
        public string ExceptionCode { get; set; }
        public string InnerExceptionCode { get; set; }
        public string ExceptionType { get; set; }
    }
}
