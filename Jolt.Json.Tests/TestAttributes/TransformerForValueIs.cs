using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

internal class TransformerForValueIs : Attribute
{
    public string NameExpression { get; } = Default.Result;
    public string ValueExpression { get; }

    public TransformerForValueIs(string valueExpression)
    {
        ValueExpression = valueExpression;
    }
}
