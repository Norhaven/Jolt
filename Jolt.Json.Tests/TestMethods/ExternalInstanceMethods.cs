using Jolt.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestMethods;

internal class ExternalInstanceMethods
{
    private readonly StringBuilder _builder = new StringBuilder();

    [JoltExternalMethod("appendString")]
    public string AppendString(string text)
    {
        _builder.Append(text);
        return _builder.ToString();
    }
}
