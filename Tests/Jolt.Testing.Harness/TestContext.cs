using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Parsing;
using Jolt.Structure;
using Jolt.Testing.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Testing.Harness
{
    public class TestContext : ITestContext
    {
        public static IJsonContext CreateNewtonsoftContext()
        {
            var messageProvider = new MessageProvider(JoltOptions.Default);

            return new JoltContext(
                default,
                new ExpressionParser(),
                new ExpressionEvaluator(JoltOptions.Default.WithUnsafeAllowed()),
                new TokenReader(messageProvider),
                new Jolt.Json.Newtonsoft.JsonTokenReader(),
                new Jolt.Json.Newtonsoft.JsonPathQueryPathProvider(),
                new MethodReferenceResolver(messageProvider),
                messageProvider,
                new ErrorHandler(default)
            );
        }

        public static IJsonContext CreateDotNetContext()
        {
            var messageProvider = new MessageProvider(JoltOptions.Default);

            return new JoltContext(
                default,
                new ExpressionParser(),
                new ExpressionEvaluator(JoltOptions.Default.WithUnsafeAllowed()),
                new TokenReader(messageProvider),
                new Jolt.Json.DotNet.JsonTokenReader(),
                new Jolt.Json.DotNet.JsonPathQueryPathProvider(),
                new MethodReferenceResolver(messageProvider),
                messageProvider,
                new ErrorHandler(default)
            );
        }

        public IJsonContext CreateJsonContext(TestType testType)
        {
            switch (testType)
            {
                case TestType.Newtonsoft: return CreateNewtonsoftContext();
                case TestType.DotNet: return CreateDotNetContext();
                default: throw new ArgumentOutOfRangeException(nameof(testType), testType, null);
            }
        }

        public IJsonObject? ParseAsJsonObject(TestType testType, string json)
        {
            return testType switch
            {
                TestType.Newtonsoft => CreateNewtonsoftContext().JsonTokenReader.Read(json) as IJsonObject,
                TestType.DotNet => CreateDotNetContext().JsonTokenReader.Read(json) as IJsonObject,
                _ => throw new ArgumentOutOfRangeException(nameof(testType), testType, null)
            };
        }
    }
}
