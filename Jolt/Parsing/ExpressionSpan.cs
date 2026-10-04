using System;

namespace Jolt.Parsing
{
    /// <summary>
    /// Represents a range of characters within the text of an expression.
    /// </summary>
    public readonly struct ExpressionSpan : IEquatable<ExpressionSpan>
    {
        /// <summary>
        /// Initializes an instance of <see cref="ExpressionSpan"/> with the provided start and length.
        /// </summary>
        /// <param name="start">The zero-based index of the first character.</param>
        /// <param name="length">The number of characters.</param>
        public ExpressionSpan(int start, int length)
        {
            Start = start;
            Length = length;
        }

        /// <summary>
        /// Gets the zero-based index of the first character.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Gets the number of characters, which may be zero for a position between characters (e.g. the end of an expression).
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets the index just past the last character.
        /// </summary>
        public int End => Start + Length;

        /// <summary>
        /// Creates a span from its start and end (exclusive) indexes.
        /// </summary>
        public static ExpressionSpan FromBounds(int start, int end) => new ExpressionSpan(start, Math.Max(0, end - start));

        /// <summary>
        /// Gets the span covering both of the given spans, or whichever of them is known.
        /// </summary>
        public static ExpressionSpan? Cover(ExpressionSpan? first, ExpressionSpan? second)
        {
            if (first is ExpressionSpan a && second is ExpressionSpan b)
            {
                return FromBounds(Math.Min(a.Start, b.Start), Math.Max(a.End, b.End));
            }

            return first ?? second;
        }

        public bool Equals(ExpressionSpan other) => Start == other.Start && Length == other.Length;

        public override bool Equals(object? obj) => obj is ExpressionSpan other && Equals(other);

        public override int GetHashCode() => (Start * 397) ^ Length;

        public override string ToString() => $"[{Start}..{End})";
    }
}
