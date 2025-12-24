using Jolt.Json.Tests.Cases.E2E.General;
using Jolt.Json.Tests.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.DotNetFramework.Tests.E2E.General.Newtonsoft
{
    public sealed class JoltTransformer : JoltTransformerTests
    {
        public JoltTransformer()
            : base(Startup.CreateNewtonsoftContext())
        {

        }
    }
}
