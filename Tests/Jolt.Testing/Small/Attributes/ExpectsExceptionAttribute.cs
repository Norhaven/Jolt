using Jolt.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Jolt.Json.Tests.Resources.TestAttributes
{
    /// <summary>
    /// Creates the expectation that an exception will be thrown of a particular exception code or type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ExpectsExceptionAttribute : Attribute, IXunitSerializable
    {
        /// <summary>
        /// Gets the expected exception code.
        /// </summary>
        public ExceptionCode Code { get; private set; }

        /// <summary>
        /// Gets the expected exception type.
        /// </summary>
        public Type ExceptionType { get; private set; }

        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public ExpectsExceptionAttribute()
        {
        }

        public ExpectsExceptionAttribute(ExceptionCode code)
        {
            Code = code;
        }

        public ExpectsExceptionAttribute(Type exceptionType)
        {
            ExceptionType = exceptionType;
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Code = info.GetValue<ExceptionCode>(nameof(Code));

            var exceptionType = info.GetValue<string>(nameof(ExceptionType));

            if (!string.IsNullOrWhiteSpace(exceptionType))
            {
                ExceptionType = Type.GetType(exceptionType);
            }
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(Code), Code);
            info.AddValue(nameof(ExceptionType), ExceptionType?.AssemblyQualifiedName);
        }
    }
}
