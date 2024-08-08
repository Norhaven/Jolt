using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Json.Tests.TestAttributes;

internal enum SourceValueType
{
    Unknown,
    StringLiteral,
    IntegerLiteral,
    DecimalLiteral,
    BooleanLiteral,
    Object
}
