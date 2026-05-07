using Jolt.Testing.Extensions;
using Jolt.Testing.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Jolt.Testing
{
    public sealed class JoltTestCase<TData> : XunitTestCase where TData : TestData, IXunitSerializable, new()
    {
        public Guid TestCaseId { get; set; }
        public TData Data { get; set; }

        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public JoltTestCase()
        {
        }

        public JoltTestCase(
            TData data,
            IMessageSink diagnosticMessageSink, 
            ITestFrameworkDiscoveryOptions discoveryOptions, 
            ITestMethod testMethod)
            : base(diagnosticMessageSink, 
                  discoveryOptions.MethodDisplayOrDefault(), 
                  discoveryOptions.MethodDisplayOptionsOrDefault(),
                  testMethod, 
                  new object[] { data })
        {
            TestCaseId = Guid.NewGuid();
            Data = data;
            Data.Messages = diagnosticMessageSink;
            DisplayName = data.TestIndex < 0 ? $"{data.TestGroup} :: {data.Name}" : $"[{data.TestIndex}] {data.TestGroup} :: {data.Name}";
        }

        public override void Serialize(IXunitSerializationInfo info)
        {
            DiagnosticMessageSink.OnMessage(new DiagnosticMessage($"Serializing into type '{info.GetType()}'"));
            DiagnosticMessageSink.OnMessage(new DiagnosticMessage($"Serializing test case with data: {Data.TestContext.GetType().Name}::{Data.TestType}::{Data.TestGroup}::{Data.Name}"));
            
            base.Serialize(info);

            info.AddValue(nameof(TestCaseId), TestCaseId.ToString());
            info.AddValue(nameof(Data), Data);
        }

        public override void Deserialize(IXunitSerializationInfo info)
        {
            base.Deserialize(info);

            TestCaseId = Guid.Parse(info.GetValue<string>(nameof(TestCaseId)));
            Data = info.GetValue<TData>(nameof(Data));

            DiagnosticMessageSink.OnMessage(new DiagnosticMessage($"Deserialized test case with data: {Data.TestContext.GetType().Name}::{Data.TestType}::{Data.TestGroup}::{Data.Name}"));
        }
    }
}
