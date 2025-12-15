using Jolt.Json.Tests.Cases.E2E.SmallTests;
using Jolt.Json.Tests.Resources;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jolt.Json.DotNetFramework.Tests.E2E.SmallTests.DotNet
{
    public sealed class ValueOf : ValueOfTests
    {
        public ValueOf()
            :base(Startup.CreateDotNetContext())
        {

        }
    }
}
