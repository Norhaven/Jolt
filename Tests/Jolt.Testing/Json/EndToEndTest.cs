using Jolt.Structure;
using Jolt.Testing.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Xunit.Abstractions;

namespace Jolt.Testing.Json
{
    [DebuggerDisplay("{TestGroup} :: {Name}")]
    public sealed class EndToEndTest : TestData
    {
        public Dictionary<string, string> PossibleExceptions { get; set; }
        public Dictionary<string, string> PossibleExternalMethodSources { get; set; }
        public string Source { get; set; }
        public string Transformer { get; set; }
        public string Result { get; set; }
        public string ExceptionCode { get; set; }
        public string InnerExceptionCode { get; set; }
        public string ExceptionType { get; set; }
        public string ExternalMethodSource { get; set; }

        [Obsolete("Used only for serialization purposes", true)]
        public EndToEndTest()
        {
        }

        public EndToEndTest(IMessageSink messages, ITestContext testContext, TestType testType, string testGroup, string testName, int testIndex)
            : base(messages, testContext, testType, testGroup, testName, testIndex)
        {
        }

        public override void Serialize(IXunitSerializationInfo info)
        {
            info.SerializeFrom(this, Messages);
        }

        public override void Deserialize(IXunitSerializationInfo info)
        {
            info.DeserializeInto(this, Messages);
        }
    }
}
