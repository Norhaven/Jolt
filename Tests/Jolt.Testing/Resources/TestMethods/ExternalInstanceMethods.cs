using Jolt.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jolt.Testing.Resources.TestMethods
{
    public class ExternalInstanceMethods
    {
        private readonly StringBuilder _builder = new StringBuilder();

        public string AppendString(string text)
        {
            _builder.Append(text);
            return _builder.ToString();
        }
    }
}
