using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Parsing;
using Jolt.Structure;
using Jolt.Testing.Json;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Testing.Harness.DotNetFramework
{
    public class TestContext : ITestContext
    {
        public static IJsonContext CreateNewtonsoftContext(JoltOptions options = default)
        {
            options = options is null ? JoltOptions.Default : options;

            var messageProvider = new MessageProvider(options);

            return new JoltContext(
                default,
                new ExpressionParser(),
                new ExpressionEvaluator(options.WithUnsafeAllowed()),
                new TokenReader(messageProvider),
                new Jolt.Json.Newtonsoft.JsonTokenReader(),
                new Jolt.Json.Newtonsoft.JsonPathQueryPathProvider(),
                new MethodReferenceResolver(messageProvider),
                messageProvider,
                new ErrorHandler(options)
            );
        }

        public static IJsonContext CreateDotNetContext(JoltOptions options = default)
        {
            options = options is null ? JoltOptions.Default : options;

            var messageProvider = new MessageProvider(options);

            return new JoltContext(
                default,
                new ExpressionParser(),
                new ExpressionEvaluator(options.WithUnsafeAllowed()),
                new TokenReader(messageProvider),
                new Jolt.Json.DotNet.JsonTokenReader(),
                new Jolt.Json.DotNet.JsonPathQueryPathProvider(),
                new MethodReferenceResolver(messageProvider),
                messageProvider,
                new ErrorHandler(options)
            );
        }

        public IJsonContext CreateJsonContext(TestType testType, JoltOptions options = default)
        {
            switch (testType)
            {
                case TestType.Newtonsoft: return CreateNewtonsoftContext(options);
                case TestType.DotNet: return CreateDotNetContext(options);
                default: throw new ArgumentOutOfRangeException(nameof(testType), testType, null);
            }
        }
    }
}
