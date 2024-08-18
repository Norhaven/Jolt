using Jolt.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

[AttributeUsage(AttributeTargets.Method)]
internal class ExpectsExceptionAttribute : Attribute
{
    public ExceptionCode Code { get; }
    public Type? ExceptionType { get; }

    public ExpectsExceptionAttribute(ExceptionCode code)
    {
        Code = code;
    }

    public ExpectsExceptionAttribute(Type? exceptionType)
    {
        ExceptionType = exceptionType;
    }
}
