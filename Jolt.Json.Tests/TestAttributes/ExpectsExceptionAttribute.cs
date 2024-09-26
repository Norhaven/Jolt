using Jolt.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

/// <summary>
/// Creates the expectation that an exception will be thrown of a particular exception code or type.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal class ExpectsExceptionAttribute : Attribute
{
    /// <summary>
    /// Gets the expected exception code.
    /// </summary>
    public ExceptionCode Code { get; }

    /// <summary>
    /// Gets the expected exception type.
    /// </summary>
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
