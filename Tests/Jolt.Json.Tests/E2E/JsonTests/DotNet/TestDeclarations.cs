using Jolt.Json.Tests.Cases.E2E.Json;
using Jolt.Json.Tests.E2E.General;
using Jolt.Json.Tests.Resources;
using Jolt.Testing;
using Jolt.Testing.Json;
using Jolt.Testing.Json.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Xunit.DependencyInjection;

namespace Jolt.Json.Tests.E2E.JsonTests.DotNet;

public sealed class JsonTestHarness
{
    //public sealed class DotNetJsonTestContainer : JsonTestContainer
    //{
    //    public DotNetJsonTestContainer(MethodInfo testMethod, JsonTestDefinitionAttribute testAttribute) 
    //        : base(testMethod, testAttribute)
    //    {
    //    }

    //    public override IJsonContext Context => Startup.CreateDotNetContext();
    //}

    //[Theory]
    [JsonTest("IndexingWithVariables", typeof(Startup), TestType.DotNet)]
    public void IndexingWithVariablesTests(string name) { Console.WriteLine(name); }
     
    //[Theory]
    //[MemberData(nameof(GetAllTestsInScope), typeof(JsonTestHarness), typeof(JsonTestDefinitionAttribute), typeof(DotNetJsonTestContainer))]
    //public void JsonTests_WillSucceed(DotNetJsonTestContainer container, EndToEndTest test) => container.Execute(test);
}

