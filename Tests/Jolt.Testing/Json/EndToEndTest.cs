using Jolt.Structure;
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
        public IJsonObject Source { get; set; }
        public IJsonObject Transformer { get; set; }
        public IJsonObject Result { get; set; }
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

        public override void Deserialize(IXunitSerializationInfo info)
        {
            base.Deserialize(info);

            var possibleExceptions = info.GetValue<string>(nameof(PossibleExceptions));
            var possibleExternalMethodSources = info.GetValue<string>(nameof(PossibleExternalMethodSources));

            var context = TestContext.CreateJsonContext(TestType);

            if (!string.IsNullOrWhiteSpace(possibleExceptions))
            {
                PossibleExceptions = context.JsonTokenReader.Read(possibleExceptions).AsObject().ToTypeOf<Dictionary<string, string>>();
            }

            if (!string.IsNullOrWhiteSpace(possibleExternalMethodSources))
            {
                PossibleExternalMethodSources = context.JsonTokenReader.Read(possibleExternalMethodSources).AsObject().ToTypeOf<Dictionary<string, string>>();
            }

            var sourceJson = info.GetValue<string>(nameof(Source));
            var transformerJson = info.GetValue<string>(nameof(Transformer));
            var result = info.GetValue<string>(nameof(Result));

            if (!string.IsNullOrWhiteSpace(sourceJson))
            {
                Source = context.JsonTokenReader.Read(sourceJson).AsObject();
            }

            if (!string.IsNullOrWhiteSpace(result))
            {
                Result = context.JsonTokenReader.Read(result).AsObject();
            }

            Transformer = context.JsonTokenReader.Read(transformerJson).AsObject();
            ExceptionCode = info.GetValue<string>(nameof(ExceptionCode));
            InnerExceptionCode = info.GetValue<string>(nameof(InnerExceptionCode));
            ExceptionType = info.GetValue<string>(nameof(ExceptionType));
            ExternalMethodSource = info.GetValue<string>(nameof(ExternalMethodSource));
        }

        public override void Serialize(IXunitSerializationInfo info)
        {
            base.Serialize(info);

            var context = TestContext.CreateJsonContext(TestType);

            if (PossibleExceptions != null)
            {
                info.AddValue(nameof(PossibleExceptions), context.JsonTokenReader.CreateTokenFrom(PossibleExceptions).ToTypeOf<string>());
            }

            if (PossibleExternalMethodSources != null)
            {
                info.AddValue(nameof(PossibleExternalMethodSources), context.JsonTokenReader.CreateTokenFrom(PossibleExternalMethodSources).ToTypeOf<string>());
            }            
            
            info.AddValue(nameof(Source), Source?.ToTypeOf<string>());
            info.AddValue(nameof(Transformer), Transformer.ToTypeOf<string>());
            info.AddValue(nameof(Result), Result?.ToTypeOf<string>());
            info.AddValue(nameof(ExceptionCode), ExceptionCode);
            info.AddValue(nameof(InnerExceptionCode), InnerExceptionCode);
            info.AddValue(nameof(ExceptionType), ExceptionType);
            info.AddValue(nameof(ExternalMethodSource), ExternalMethodSource);
        }
    }
}
