using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Jolt.Extensions
{
    internal static class DescriptiveExtensions
    {
        public static string? GetDescription<T>(this T value) where T : Enum
        {
            var member = typeof(T).GetMember(value.ToString());

            if (!member.Any())
            {
                return default;
            }

            var attribute = member[0].GetCustomAttribute<DescriptionAttribute>();

            return attribute?.Description ?? default;
        }
    }
}
