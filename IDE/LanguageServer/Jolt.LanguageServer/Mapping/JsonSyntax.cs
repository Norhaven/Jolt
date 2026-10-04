using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Jolt.LanguageServer.Mapping
{
    /// <summary>
    /// Represents a JSON value along with where it is in the document text. Offsets are indexes into the text
    /// (UTF-16 code units), with <see cref="End"/> exclusive.
    /// </summary>
    internal abstract class JsonSyntaxNode
    {
        public int Start { get; set; }

        public int End { get; set; }
    }

    internal sealed class JsonObjectSyntax : JsonSyntaxNode
    {
        public List<JsonPropertySyntax> Properties { get; } = new List<JsonPropertySyntax>();

        public JsonSyntaxNode? this[string name] => Properties.Find(x => x.Key.Value == name)?.Value;
    }

    internal sealed class JsonPropertySyntax
    {
        public JsonPropertySyntax(JsonStringSyntax key, JsonSyntaxNode value)
        {
            Key = key;
            Value = value;
        }

        public JsonStringSyntax Key { get; }

        public JsonSyntaxNode Value { get; }
    }

    internal sealed class JsonArraySyntax : JsonSyntaxNode
    {
        public List<JsonSyntaxNode> Items { get; } = new List<JsonSyntaxNode>();
    }

    /// <summary>
    /// Represents a string, including its quotes in <see cref="JsonSyntaxNode.Start"/> and <see cref="JsonSyntaxNode.End"/>.
    /// </summary>
    internal sealed class JsonStringSyntax : JsonSyntaxNode
    {
        public JsonStringSyntax(string value, int[] rawOffsets)
        {
            Value = value;
            RawOffsets = rawOffsets;
        }

        /// <summary>
        /// Gets the decoded string value, with escape sequences resolved.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the document offset where each character of <see cref="Value"/> starts, which differs from a simple
        /// offset when there are escape sequences. The final element is the offset of the closing quote, so that the
        /// end of any range within the value can be mapped as well.
        /// </summary>
        public int[] RawOffsets { get; }
    }

    internal sealed class JsonLiteralSyntax : JsonSyntaxNode
    {
    }

    internal sealed class JsonSyntaxException : Exception
    {
        public JsonSyntaxException(string message, int offset)
            : base(message)
        {
            Offset = offset;
        }

        public int Offset { get; }
    }

    /// <summary>
    /// Parses strict JSON (as accepted by System.Text.Json, which Jolt uses) into a tree that keeps the location of
    /// each value, which the validation issues reported by Jolt do not include.
    /// </summary>
    internal sealed class JsonSyntaxParser
    {
        private readonly string _text;
        private int _position;

        private JsonSyntaxParser(string text)
        {
            _text = text;
        }

        /// <summary>
        /// Parses the given text as a single JSON value.
        /// </summary>
        /// <exception cref="JsonSyntaxException">The text is not valid JSON.</exception>
        public static JsonSyntaxNode Parse(string text)
        {
            var parser = new JsonSyntaxParser(text);

            // A byte order mark is not part of the content.
            if (text.Length > 0 && text[0] == '﻿')
            {
                parser._position = 1;
            }

            parser.SkipWhitespace();

            var value = parser.ParseValue();

            parser.SkipWhitespace();

            if (parser._position < text.Length)
            {
                throw new JsonSyntaxException("Unexpected content after the end of the JSON document.", parser._position);
            }

            return value;
        }

        private JsonSyntaxNode ParseValue()
        {
            if (_position >= _text.Length)
            {
                throw new JsonSyntaxException("Expected a JSON value but found the end of the document.", _position);
            }

            switch (_text[_position])
            {
                case '{':
                    return ParseObject();
                case '[':
                    return ParseArray();
                case '"':
                    return ParseString();
                case 't':
                    return ParseKeyword("true");
                case 'f':
                    return ParseKeyword("false");
                case 'n':
                    return ParseKeyword("null");
                case var c when c == '-' || (c >= '0' && c <= '9'):
                    return ParseNumber();
                default:
                    throw new JsonSyntaxException($"Unexpected character '{_text[_position]}', expected a JSON value.", _position);
            }
        }

        private JsonObjectSyntax ParseObject()
        {
            var obj = new JsonObjectSyntax { Start = _position };

            _position++;
            SkipWhitespace();

            if (TryConsume('}'))
            {
                obj.End = _position;
                return obj;
            }

            while (true)
            {
                if (_position >= _text.Length || _text[_position] != '"')
                {
                    throw new JsonSyntaxException("Expected a property name in double quotes.", _position);
                }

                var key = ParseString();

                SkipWhitespace();
                Expect(':', "Expected ':' after the property name.");
                SkipWhitespace();

                obj.Properties.Add(new JsonPropertySyntax(key, ParseValue()));

                SkipWhitespace();

                if (TryConsume('}'))
                {
                    obj.End = _position;
                    return obj;
                }

                Expect(',', "Expected ',' or '}' after the property value.");
                SkipWhitespace();
            }
        }

        private JsonArraySyntax ParseArray()
        {
            var array = new JsonArraySyntax { Start = _position };

            _position++;
            SkipWhitespace();

            if (TryConsume(']'))
            {
                array.End = _position;
                return array;
            }

            while (true)
            {
                array.Items.Add(ParseValue());

                SkipWhitespace();

                if (TryConsume(']'))
                {
                    array.End = _position;
                    return array;
                }

                Expect(',', "Expected ',' or ']' after the array element.");
                SkipWhitespace();
            }
        }

        private JsonStringSyntax ParseString()
        {
            var start = _position;
            var value = new StringBuilder();
            var rawOffsets = new List<int>();

            _position++;

            while (true)
            {
                if (_position >= _text.Length)
                {
                    throw new JsonSyntaxException("Unterminated string, expected a closing '\"'.", start);
                }

                var c = _text[_position];

                if (c == '"')
                {
                    rawOffsets.Add(_position);
                    _position++;

                    return new JsonStringSyntax(value.ToString(), rawOffsets.ToArray()) { Start = start, End = _position };
                }

                if (c < ' ')
                {
                    throw new JsonSyntaxException("Control characters, such as line breaks, must be escaped within a string.", _position);
                }

                rawOffsets.Add(_position);

                if (c != '\\')
                {
                    value.Append(c);
                    _position++;
                    continue;
                }

                if (_position + 1 >= _text.Length)
                {
                    throw new JsonSyntaxException("Unterminated string, expected a closing '\"'.", start);
                }

                var escaped = _text[_position + 1];

                switch (escaped)
                {
                    case '"': value.Append('"'); break;
                    case '\\': value.Append('\\'); break;
                    case '/': value.Append('/'); break;
                    case 'b': value.Append('\b'); break;
                    case 'f': value.Append('\f'); break;
                    case 'n': value.Append('\n'); break;
                    case 'r': value.Append('\r'); break;
                    case 't': value.Append('\t'); break;
                    case 'u':
                        if (_position + 6 > _text.Length || !ushort.TryParse(_text.Substring(_position + 2, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                        {
                            throw new JsonSyntaxException("Invalid unicode escape sequence, expected four hexadecimal digits after '\\u'.", _position);
                        }

                        value.Append((char)code);
                        _position += 6;
                        continue;
                    default:
                        throw new JsonSyntaxException($"Invalid escape sequence '\\{escaped}'.", _position);
                }

                _position += 2;
            }
        }

        private JsonLiteralSyntax ParseKeyword(string keyword)
        {
            if (string.CompareOrdinal(_text, _position, keyword, 0, keyword.Length) != 0)
            {
                throw new JsonSyntaxException($"Unexpected character '{_text[_position]}', expected a JSON value.", _position);
            }

            var literal = new JsonLiteralSyntax { Start = _position, End = _position + keyword.Length };

            _position += keyword.Length;

            return literal;
        }

        private JsonLiteralSyntax ParseNumber()
        {
            var start = _position;

            TryConsume('-');

            if (TryConsume('0'))
            {
                // A leading zero cannot be followed by other digits.
            }
            else if (!ConsumeDigits())
            {
                throw new JsonSyntaxException("Invalid number, expected a digit.", _position);
            }

            if (TryConsume('.') && !ConsumeDigits())
            {
                throw new JsonSyntaxException("Invalid number, expected a digit after the decimal point.", _position);
            }

            if (TryConsume('e') || TryConsume('E'))
            {
                if (!TryConsume('+'))
                {
                    TryConsume('-');
                }

                if (!ConsumeDigits())
                {
                    throw new JsonSyntaxException("Invalid number, expected a digit in the exponent.", _position);
                }
            }

            return new JsonLiteralSyntax { Start = start, End = _position };
        }

        private bool ConsumeDigits()
        {
            var start = _position;

            while (_position < _text.Length && _text[_position] >= '0' && _text[_position] <= '9')
            {
                _position++;
            }

            return _position > start;
        }

        private void SkipWhitespace()
        {
            while (_position < _text.Length && (_text[_position] == ' ' || _text[_position] == '\t' || _text[_position] == '\n' || _text[_position] == '\r'))
            {
                _position++;
            }
        }

        private bool TryConsume(char c)
        {
            if (_position < _text.Length && _text[_position] == c)
            {
                _position++;
                return true;
            }

            return false;
        }

        private void Expect(char c, string message)
        {
            if (!TryConsume(c))
            {
                throw new JsonSyntaxException(message, _position);
            }
        }
    }
}
