using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.Resources.TestAttributes
{
    /// <summary>
    /// Creates the expectation that the specified JSON object will be returned as the result.
    /// </summary>
    public sealed class ExpectsResultOfJsonObjectAttribute : ExpectsResultAttribute
    {
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public ExpectsResultOfJsonObjectAttribute()
        {
        }

        public ExpectsResultOfJsonObjectAttribute(string value)
            : base(Default.Result, value)
        {
        }

        public ExpectsResultOfJsonObjectAttribute(string propertyName, string value)
            : base(propertyName, value)
        {
        }
    }
}
