using Jolt.Json.Tests.Resources.TestAttributes;
using Jolt.Structure;
using Jolt.Testing.Extensions;
using Jolt.Testing.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace Jolt.Testing.Small
{
    public sealed class SmallTest : TestData
    {
        public string SourceJson { get; set; }
        public string TransformerJson { get; set; }
        public ExpectsResultAttribute ExpectsResult { get; set; }
        public ExpectsExceptionAttribute ExpectsException { get; set; }

        [Obsolete("Used for serialization only", true)]
        public SmallTest()
        {
        }

        public SmallTest(IMessageSink messages, ITestContext testContext, TestType testType, string testGroup, string testName, int testIndex, string sourceJson, string transformerJson, ExpectsResultAttribute expectsResult, ExpectsExceptionAttribute expectsException)
            : base(messages, testContext, testType, testGroup, testName, testIndex)
        {
            SourceJson = sourceJson;
            TransformerJson = transformerJson;
            ExpectsResult = expectsResult;
            ExpectsException = expectsException;
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
