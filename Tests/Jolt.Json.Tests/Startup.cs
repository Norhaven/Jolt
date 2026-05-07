using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Json.Tests.Resources;
using Jolt.Parsing;
using Jolt.Testing;
using Jolt.Testing.Json;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Json.Tests;

public class Startup : IJsonContextFactory
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

    public IJsonContext CreateJsonContext(TestType testType)
    {
        switch (testType)
        {
            case TestType.Newtonsoft: return CreateNewtonsoftContext();
            case TestType.DotNet: return CreateDotNetContext();
            default: throw new ArgumentOutOfRangeException(nameof(testType), testType, null);
        }
    }
}
