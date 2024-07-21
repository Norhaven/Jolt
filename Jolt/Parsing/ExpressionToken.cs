using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing
{
    public class ExpressionToken
    {
        public const char OpenParentheses = '(';
        public const char CloseParentheses = ')';
        public const char Hash = '#';
        public const char Comma = ',';
        public const char SingleQuote = '\'';
        public const char DollarSign = '$';
        public const char Whitespace = ' ';
        public const char ArrowBody = '-';
        public const char ArrowHead = '>';

        public string Value { get; }
        public ExpressionTokenCategory Category { get; }

        public ExpressionToken(string value, ExpressionTokenCategory category)
        {
            Value = value;
            Category = category;
        }
    }
}
