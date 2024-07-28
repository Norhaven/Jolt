using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestMethods;

public static class ExternalStaticMethods
{
    public static bool TakesAndReturnsBoolean(bool value) => value;
    public static string Concatenate(string first, string second) => first + second;
}
