using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

internal class ExpectsExceptionAttribute : Attribute
{
    public Type? ExceptionType { get; }

    public ExpectsExceptionAttribute(Type? exceptionType)
    {
        ExceptionType = exceptionType;
    }
}
