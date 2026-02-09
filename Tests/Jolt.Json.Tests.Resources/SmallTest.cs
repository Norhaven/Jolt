using FluentAssertions;
using Jolt.Exceptions;
using Jolt.Structure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Jolt.Json.Tests.Resources.TestAttributes;
using Jolt.Json.Tests.Resources;
using Jolt.Library;

namespace Jolt.Json.Tests.Resources
{
    public abstract class SmallTest : Test
    {
        public sealed class SmallTestContainer : TestContainer
        {
            public SmallTestContainer(MethodInfo testMethod, SmallTestDefinitionAttribute attribute)
                : base(testMethod, attribute)
            {
            }

            public override void Execute(IJsonContext context)
            {
                var source = _method.GetCustomAttribute<SourceHasAttribute>();
                var target = _method.GetCustomAttribute<TransformerIsAttribute>();
                var expectsResult = _method.GetCustomAttribute<ExpectsResultAttribute>();
                var expectsException = _method.GetCustomAttribute<ExpectsExceptionAttribute>();

                if (_method is null)
                {
                    throw new ArgumentNullException(nameof(_method), $"Unable to locate test method '{_method}'");
                }

                if (source is null || target is null)
                {
                    throw new ArgumentNullException(nameof(_method), $"Either source or transformer is missing for test method '{_method}'");
                }

                var reader = context.JsonTokenReader;

                var sourceJson = reader.Read("{}") as IJsonObject;
                var transformerJson = reader.Read("{}") as IJsonObject;

                sourceJson[source.Name] = source.Type switch
                {
                    SourceValueType.Object => reader.Read(source.Value?.ToString()),
                    _ => reader.CreateTokenFrom(source.Value)
                };

                transformerJson[target.NameExpression] = reader.CreateTokenFrom(target.ValueExpression);

                var currentContext = context
                    .UseTransformer(transformerJson.ToString());

                var transformer = new JoltTransformer<IJsonContext>(currentContext);

                try
                {
                    var result = transformer.Transform(sourceJson.ToString());

                    var jsonResult = reader.Read(result) as IJsonObject;
                    var value = jsonResult[expectsResult.PropertyName];

                    if (value is null && expectsResult.Value is null)
                    {
                        return;
                    }

                    value.Should().NotBeNull($"because we are expecting a value '{expectsResult.Value}' instead");

                    if (value.Type == JsonTokenType.Object)
                    {
                        var expectedToken = reader.Read(expectsResult.Value?.ToString());
                        value.Equals(expectedToken).Should().BeTrue("because the transformed JSON should match the expectation");
                    }
                    else
                    {
                        value.ToTypeOf<object>().Should().Be(expectsResult.Value, "because the result was expected by the test");
                    }
                }
                catch (JoltException ex)
                {
                    if (expectsException is null)
                    {
                        throw;
                    }

                    if (expectsException.ExceptionType is null)
                    {
                        expectsException.Code.Should().Be(ex.Code, "because this exception code was expected");
                    }
                    else
                    {
                        expectsException.ExceptionType.Should().Be(ex.GetType(), "because this exception was expected");
                    }
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

        public SmallTest(Func<IJsonContext> context)
            :base(context)
        {
        }
    }
}