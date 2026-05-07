using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Jolt.Json.Tests.Resources.TestAttributes
{
    /// <summary>
    /// Creates the expectation that a specific value will be returned as the result in the specified property (if applicable).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ExpectsResultAttribute : Attribute, IXunitSerializable
    {
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public ExpectsResultAttribute()
        {
        }

        /// <summary>
        /// Gets the name of the property that should contain the expected value.
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Gets the expected value.
        /// </summary>
        public object Value { get; set; }

        public ExpectsResultAttribute(object value)
        {
            PropertyName = Default.Result;
            Value = value;
        }

        public ExpectsResultAttribute(string propertyName, object value)
        {
            PropertyName = propertyName;
            Value = value;
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            PropertyName = info.GetValue<string>(nameof(PropertyName));
            Value = info.GetValue<object>(nameof(Value));
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(PropertyName), PropertyName);
            info.AddValue(nameof(Value), Value);
        }
    }
}