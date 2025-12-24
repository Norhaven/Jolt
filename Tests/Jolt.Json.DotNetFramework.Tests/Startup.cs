using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Json.Tests.Resources;
using Jolt.Parsing;
using Jolt.Structure;
using System;
using Xunit;

namespace Jolt.Json.DotNetFramework.Tests
{
    public class Startup
    {
        public static IJsonContext CreateNewtonsoftContext()
        {
            var messageProvider = new MessageProvider(JoltOptions.Default);

            return new JoltContext(
                default,
                new ExpressionParser(),
                new ExpressionEvaluator(),
                new TokenReader(messageProvider),
                new Newtonsoft.JsonTokenReader(),
                new Newtonsoft.JsonPathQueryPathProvider(),
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
                new ExpressionEvaluator(),
                new TokenReader(messageProvider),
                new DotNet.JsonTokenReader(),
                new DotNet.JsonPathQueryPathProvider(),
                new MethodReferenceResolver(messageProvider),
                messageProvider,
                new ErrorHandler(default)
            );
        }
    }
}