using Jolt.Json.DotNet;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests;

public sealed class DotNetTests : JoltJsonTransformerTests
{
    protected override IJsonTokenReader CreateTokenReader() => new JsonTokenReader();
    protected override IJsonTransformer<IJsonContext> CreateTransformer(JoltContext context) => new JoltJsonTransformer(context);
    protected override IQueryPathProvider CreateQueryPathProvider() => new JsonPathQueryPathProvider();
    protected override IJsonObject ParseJson(string json) => JsonToken.Parse(json).AsObject();
}
