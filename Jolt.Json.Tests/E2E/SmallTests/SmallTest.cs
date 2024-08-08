using FluentAssertions;
using Jolt.Json.Tests.TestAttributes;
using Jolt.Structure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit.DependencyInjection;

namespace Jolt.Json.Tests.E2E.SmallTests;

public abstract class SmallTest(IJsonContext context) : Test(context)
{
    protected void ExecuteSmallTest([CallerMemberName] string methodName = default)
    {
        var method = GetType().GetMethod(methodName);
        var source = method.GetCustomAttribute<SourceHasAttribute>();
        var target = method.GetCustomAttribute<TransformerIsAttribute>();
        var expectsResult = method.GetCustomAttribute<ExpectsResultAttribute>();
        var expectsException = method.GetCustomAttribute<ExpectsExceptionAttribute>();

        if (method is null)
        {
            throw new ArgumentNullException(nameof(method), $"Unable to locate test method '{methodName}'");
        }

        if (source is null || target is null)
        {
            throw new ArgumentNullException(nameof(source), $"Either source or transformer is missing for test method '{methodName}'");
        }

        var reader = _testContext.JsonTokenReader;

        var sourceJson = reader.Read("{}") as IJsonObject;
        var transformerJson = reader.Read("{}") as IJsonObject;

        sourceJson[source.Name] = reader.CreateTokenFrom(source.Value);
        transformerJson[target.NameExpression] = reader.CreateTokenFrom(target.ValueExpression);

        var transformer = CreateTransformerWith(transformerJson.ToString(), []);

        try
        {
            var result = transformer.Transform(sourceJson.ToString());

            var jsonResult = reader.Read(result) as IJsonObject;
            jsonResult[expectsResult.PropertyName].ToTypeOf<object>().Should().Be(expectsResult.Value, "because the result was expected by the test");
        }
        catch (Exception ex)
        {
            if (expectsException is null)
            {
                throw;
            }

            expectsException.ExceptionType.Should().Be(ex.GetType(), "because this exception was expected");
        }
    }
}
