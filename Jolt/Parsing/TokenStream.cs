using Jolt.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Parsing
{
    internal class TokenStream<T> : ITokenStream<T>
    {
        private readonly IEnumerator<T> _enumerator;
        private bool _isCompleted;
        private int _position;
        private int _consumedCount;

        public T CurrentToken => IsCompleted ? default : _enumerator.Current;
        public int Position => _position;
        public bool IsCompleted => _isCompleted;

        /// <summary>
        /// Gets the number of items consumed so far, which is also the index of the current item. Unlike
        /// <see cref="Position"/>, this includes items consumed in bulk (e.g. by <see cref="TryConsumeUntil"/>).
        /// </summary>
        internal int ConsumedCount => _consumedCount;

        public TokenStream(IEnumerable<T> tokens)
        {
            _enumerator = tokens.GetEnumerator();
            _isCompleted = !_enumerator.MoveNext();
            _position = 1;
        }

        public bool TryMatchNextAndConsume(Predicate<T> isMatch)
        {
            if (_isCompleted)
            {
                return false;
            }

            if (isMatch(_enumerator.Current))
            {
                MoveNext();
                return true;
            }

            return false;
        }

        public bool TryMatchNextAndConsume(Predicate<T> isMatch, out T token)
        {
            var current = _enumerator.Current;

            if (TryMatchNextAndConsume(isMatch))
            {
                token = current;
                return true;
            }

            token = default;
            return false;
        }

        public bool TryConsumeUntilMatchOrEnd(Predicate<T> isMatch, out T[] tokens)
        {
            tokens = Array.Empty<T>();

            if (_isCompleted)
            {
                return false;
            }

            var collectedTokens = new List<T>
            {
                _enumerator.Current,
            };

            while (Advance())
            {
                if (isMatch(_enumerator.Current))
                {
                    tokens = collectedTokens.ToArray();
                    return true;
                }

                collectedTokens.Add(_enumerator.Current);
            }

            tokens = collectedTokens.ToArray();

            _isCompleted = true;
            return true;
        }

        public bool TryConsumeUntil(Predicate<T> isMatch, out T[] tokens)
        {
            tokens = Array.Empty<T>();

            if (_isCompleted)
            {
                return false;
            }

            if (isMatch(_enumerator.Current))
            {
                return false;
            }

            var collectedTokens = new List<T>
            {
                _enumerator.Current,
            };

            while (Advance())
            {
                if (isMatch(_enumerator.Current))
                {
                    tokens = collectedTokens.ToArray();
                    return true;
                }

                collectedTokens.Add(_enumerator.Current);
            }

            tokens = collectedTokens.ToArray();

            _isCompleted = true;
            return false;
        }

        public bool TryConsumeNext(out T token)
        {
            token = default;

            if (_isCompleted)
            {
                return false;
            }

            token = _enumerator.Current;
            MoveNext();

            return true;
        }

        public T ConsumeCurrent()
        {
            var token = _enumerator.Current;
            MoveNext();

            return token;
        }

        public T[] ConsumeUntilEnd()
        {
            var tokens = new List<T>
            {
                _enumerator.Current
            };

            while (Advance())
            {
                tokens.Add(_enumerator.Current);
            }

            _isCompleted = true;

            return tokens.ToArray();
        }

        private void MoveNext()
        {
            if (_isCompleted)
            {
                return;
            }

            _isCompleted = !Advance();
            _position++;
        }

        private bool Advance()
        {
            _consumedCount++;
            return _enumerator.MoveNext();
        }
    }
}
