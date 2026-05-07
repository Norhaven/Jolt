using Jolt.Testing.Extensions;
using Jolt.Testing.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace Jolt.Testing
{
    public abstract class TestData : IXunitSerializable
    {
        public Guid TestDataId { get; set; }
        public ITestContext TestContext { get; set; }
        public TestType TestType { get; set; }
        public string TestGroup { get; set; }
        public string Name { get; set; }
        public int TestIndex { get; set; }
        public IMessageSink Messages { get; set; }

        public IJsonContext JsonContext => TestContext?.CreateJsonContext(TestType);

        [Obsolete("Used only for serialization purposes", true)]
        public TestData()
        {
        }

        protected TestData(IMessageSink messages, ITestContext testContext, TestType testType, string testGroup, string testName, int testIndex)
        {
            TestDataId = Guid.NewGuid();
            Messages = messages;
            TestContext = testContext;
            TestType = testType;
            TestGroup = testGroup;
            Name = testName;
            TestIndex = testIndex;
        }

        public virtual void Deserialize(IXunitSerializationInfo info)
        {
            info.DeserializeInto(this, Messages);
        }

        public virtual void Serialize(IXunitSerializationInfo info)
        {
            info.SerializeFrom(this, Messages);
        }
    }
}
