using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Testing.Json.Comparers
{
    internal sealed class TestDoubleJsonEqualityComparer : IJsonEqualityComparer
    {
        public Type ApplicableType => typeof(IJsonValue);

        public bool AreEqual(IJsonToken token, IJsonToken other)
        {
            if (token is IJsonValue tokenValue && other is IJsonValue otherValue)
            {
                var tokenString = tokenValue.ToTypeOf<string>();
                var otherString = otherValue.ToTypeOf<string>();

                if (tokenValue.Type == JsonTokenType.Value && tokenValue.ValueType == JsonValueType.Number
                    && otherValue.Type == JsonTokenType.Value && otherValue.ValueType == JsonValueType.Number)
                {
                    if (tokenString.Length != otherString.Length)
                    {
                        var tokenParts = tokenString.Split(new[] { '.', 'e', 'E' }, StringSplitOptions.RemoveEmptyEntries);
                        var otherTokenParts = otherString.Split(new[] { '.', 'e', 'E' }, StringSplitOptions.RemoveEmptyEntries);

                        // The number before the decimal point or exponent should be the same regardless of how the JSON parser
                        // might choose to represent it, so if those don't match we can just fail immediately without worrying
                        // about truncation.

                        if (tokenParts[0] != otherTokenParts[0])
                        {
                            return false;
                        }

                        if (tokenParts.Length != otherTokenParts.Length)
                        {
                            // The number may actually be a whole number but due to the way the JSON parser works it might be represented
                            // with a decimal point and some number of zeros after it, so we should consider those equal as well since they
                            // are mathematically equivalent.

                            if (tokenParts.Length == 1 && otherTokenParts[1].All(x => x == '0'))
                            {
                                return true;
                            }

                            if (otherTokenParts.Length == 1 && tokenParts[1].All(x => x == '0'))
                            {
                                return true;
                            }
                        }

                        // This is to handle cases where the JSON parser might parse a number like 1.66666 as 1.66667 or vice versa,
                        // which would cause a mismatch with the expected test result. For testing purposes, we really
                        // only care about precision being accurate to about three decimal places so let's truncate to 
                        // the shortest one and see if that works out better.

                        if (tokenParts[1].Length > otherTokenParts[1].Length)
                        {
                            tokenParts[1] = tokenParts[1].Substring(0, otherTokenParts[1].Length);
                        }
                        else
                        {
                            otherTokenParts[1] = otherTokenParts[1].Substring(0, tokenParts[1].Length);
                        }

                        var tokenDouble = double.Parse($"{tokenParts[0]}.{tokenParts[1]}");
                        var otherDouble = double.Parse($"{otherTokenParts[0]}.{otherTokenParts[1]}");

                        return Math.Round(tokenDouble, 3) == Math.Round(otherDouble, 3);
                    }
                }

                return tokenString == otherString;
            }

            return false;
        }
    }
}
