using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Parsing
{
    public class TokenStream<T>
    {
        private readonly IEnumerator<T> _enumerator;
        private bool _isCompleted;
        private int _position;

        public T CurrentToken => IsCompleted ? default : _enumerator.Current;
        public int Position => _position;
        public bool IsCompleted => _isCompleted;

        public TokenStream(IEnumerable<T> tokens)
        {
            _enumerator = tokens.GetEnumerator();
            _isCompleted = !_enumerator.MoveNext();
            _position = 1;            
        }

        public bool TryMatchNextAndConsume(Predicate<T> isMatch)
        {
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

        public bool TryConsumeUntil(Predicate<T> isMatch, out T[] tokens)
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

            while (_enumerator.MoveNext())
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

            while(_enumerator.MoveNext())
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

            _isCompleted = !_enumerator.MoveNext();
            _position++;
        }
    }
}
