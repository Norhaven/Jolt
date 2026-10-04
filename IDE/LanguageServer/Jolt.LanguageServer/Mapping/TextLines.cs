using Jolt.LanguageServer.Protocol;
using System;
using System.Collections.Generic;
using Range = Jolt.LanguageServer.Protocol.Range;

namespace Jolt.LanguageServer.Mapping
{
    /// <summary>
    /// Converts offsets in a document's text to protocol positions (line and UTF-16 character).
    /// </summary>
    internal sealed class TextLines
    {
        private readonly List<int> _lineStarts = new List<int> { 0 };

        public TextLines(string text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                // "\r\n", "\n", and a lone "\r" all end a line, as in the protocol.
                if (text[i] == '\n' || (text[i] == '\r' && (i + 1 >= text.Length || text[i + 1] != '\n')))
                {
                    _lineStarts.Add(i + 1);
                }
            }
        }

        public Position GetPosition(int offset)
        {
            var line = _lineStarts.BinarySearch(offset);

            // When the offset is not the start of a line, BinarySearch returns the complement of the next line's index.
            if (line < 0)
            {
                line = ~line - 1;
            }

            return new Position { Line = line, Character = offset - _lineStarts[line] };
        }

        public Range GetRange(int start, int end) => new Range { Start = GetPosition(start), End = GetPosition(Math.Max(start, end)) };
    }
}
