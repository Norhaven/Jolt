using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Json.Tests.Resources;
using Jolt.Parsing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Json.Tests;

public class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        IJsonContext CreateNewtonsoftContext()
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

        IJsonContext CreateDotNetContext()
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

        services
            .AddKeyedTransient(TestType.Newtonsoft, (x, _) => CreateNewtonsoftContext())
            .AddKeyedTransient(TestType.DotNet, (x, _) => CreateDotNetContext());
    }
}
