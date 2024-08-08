using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

internal class TransformerIsAttribute : Attribute
{
    public string NameExpression { get; }
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
