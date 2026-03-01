using System;

namespace Jolt.Parsing
{
    public interface ITokenStream<T>
    {
        T CurrentToken { get; }
        bool IsCompleted { get; }
        int Position { get; }

        T ConsumeCurrent();
        T[] ConsumeUntilEnd();
        bool TryConsumeNext(out T token);
        bool TryConsumeUntil(Predicate<T> isMatch, out T[] tokens);
        bool TryConsumeUntilMatchOrEnd(Predicate<T> isMatch, out T[] tokens);
        bool TryMatchNextAndConsume(Predicate<T> isMatch);
        bool TryMatchNextAndConsume(Predicate<T> isMatch, out T token);
    }
}