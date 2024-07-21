using System;

namespace Jolt
{
    public interface IJsonTransformer<out TContext> where TContext : IJsonContext
    {
        string? Transform(string json);
    }
}
