using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jolt.Testing.Harness.DotNetFramework.Diagnostics
{
    public sealed class VerifyLoadedAssemblyBindings
    {
        [Fact]
        public void DiagnosticLoadedAssemblies()
        {
            var problematicAssemblies = new[]
            {
                "System.Text.Json",
                "System.Text.Encodings.Web",
                "Microsoft.Extensions.Logging.Abstractions",
                "Microsoft.Extensions.DependencyInjection.Abstractions",
                "Microsoft.Bcl.AsyncInterfaces",
                "System.Diagnostics.DiagnosticSource"
            };

            foreach (var assemblyName in problematicAssemblies)
            {
                try
                {
                    var assembly = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.GetName().Name == assemblyName);

                    if (assembly != null)
                    {
                        var version = assembly.GetName().Version;
                        Debug.WriteLine($"{assemblyName}: {version}");
                    }
                    else
                    {
                        Debug.WriteLine($"{assemblyName}: NOT LOADED");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{assemblyName}: ERROR - {ex.Message}");
                }
            }
        }
    }
}
