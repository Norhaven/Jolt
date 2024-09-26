using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

/// <summary>
/// Indicates that the transformer should be composed of a specific JSON string on the property name and/or value side.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal class TransformerIsAttribute : Attribute
{
    /// <summary>
    /// Gets the expression contained within the property name.
    /// </summary>
    public string NameExpression { get; }

    /// <summary>
    /// Gets the expression contained within the property value.
    /// </summary>
    public string ValueExpression { get; }

    public TransformerIsAttribute(string valueExpression)
    {
        NameExpression = Default.Result;
        ValueExpression = valueExpression;
    }

    public TransformerIsAttribute(string nameExpression, string valueExpression)
    {
        NameExpression = nameExpression;
        ValueExpression = valueExpression;
    }
}
