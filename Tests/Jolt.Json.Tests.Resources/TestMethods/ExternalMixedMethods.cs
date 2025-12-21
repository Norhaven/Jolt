using Jolt.Library;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Json.Tests.Resources.TestMethods
{
    public class ExternalMixedMethods
    {
        private readonly StringBuilder _builder = new StringBuilder();

        [JoltExternalMethod("aliasedAppend")]
        public string AppendWithAlias(string text)
        {
            _builder.Append(text);
            return _builder.ToString();
        }

        [JoltExternalMethod("booleanPassthrough")]
        public static bool PassBooleanThroughAndReturn(bool value) => value;

        [JoltExternalMethod("stringConcat")]
        public static string StringConcatenation(string first, string second) => first + second;
    }
}
