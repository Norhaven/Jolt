using Jolt.Testing.Extensions;
using Jolt.Testing.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace Jolt.Testing.Transformers
{
    public sealed class TransformerTest : TestData
    {
        public string Transformer { get; set; }
        public string Source { get; set; }
        public Type ExternalMethodsType { get; set; }

        [Obsolete("Only used for serialization, do not use otherwise.")]
        public TransformerTest()
        {
        }

        public TransformerTest(string transformer, string source, Type externalMethodsType, IMessageSink messages, ITestContext testContext, TestType testType, string testGroup, string testName, int testIndex) 
            : base(messages, testContext, testType, testGroup, testName, testIndex)
        {
            Transformer = transformer;
            Source = source;
            ExternalMethodsType = externalMethodsType;
        }

        public override void Deserialize(IXunitSerializationInfo info)
        {
            base.Deserialize(info);

            info.DeserializeInto(this, Messages);
        }

        public override void Serialize(IXunitSerializationInfo info)
        {
            base.Serialize(info);

            info.SerializeFrom(this, Messages);
        }
    }
}
