using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

/// <summary>
/// Creates the expectation that the specified JSON object will be returned as the result.
/// </summary>
internal class ExpectsResultOfJsonObjectAttribute : ExpectsResultAttribute
{
    public ExpectsResultOfJsonObjectAttribute(string? value)
        : base(Default.Result, value)
    {
    }

    public ExpectsResultOfJsonObjectAttribute(string propertyName, string? value)
        : base(propertyName, value)
    {
    }
}
