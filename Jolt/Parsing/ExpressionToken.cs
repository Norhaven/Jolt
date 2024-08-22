using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Parsing
{
    public sealed class ExpressionToken
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
        public const char Equal = '=';
        public const char Plus = '+';
        public const char Minus = '-';
        public const char GreaterThan = '>';
        public const char LessThan = '<';
        public const char Star = '*';
        public const char ForwardSlash = '/';
        public const char Not = '!';
        public const char DecimalPoint = '.';
        public const char RangeEndIndexer = '^';
        public const char At = '@';
        public const char Colon = ':';
        public const char Dot = '.';
        public const string In = "in";
        public const string As = "as";
        public const char LetterI = 'i';
        public const char LetterA = 'a';
        public const char Semicolon = ';';

        public string Value { get; }
        public ExpressionTokenCategory Category { get; }

        public ExpressionToken(string value, ExpressionTokenCategory category)
        {
            Value = value;
            Category = category;
        }
    }
}
