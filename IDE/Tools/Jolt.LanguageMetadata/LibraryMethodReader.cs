using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Jolt.LanguageMetadata
{
    /// <summary>
    /// Reads the standard library method definitions from the Jolt assembly. The attributes involved are internal to Jolt,
    /// so they are matched by name through <see cref="CustomAttributeData"/> rather than by type.
    /// </summary>
    internal static class LibraryMethodReader
    {
        private const string IncludeInStandardLibraryAttribute = "Jolt.Library.IncludeInStandardLibraryAttribute";
        private const string JoltLibraryMethodAttribute = "Jolt.Library.JoltLibraryMethodAttribute";
        private const string MethodIsValidOnAttribute = "Jolt.Library.MethodIsValidOnAttribute";
        private const string OptionalParameterAttribute = "Jolt.Library.OptionalParameterAttribute";
        private const string VariadicEvaluationAttribute = "Jolt.Library.VariadicEvaluationAttribute";
        private const string LazyEvaluationAttribute = "Jolt.Library.LazyEvaluationAttribute";
        private const string EvaluationContextType = "Jolt.Evaluation.EvaluationContext";

        public static List<LibraryMethodMetadata> ReadFrom(Assembly assembly)
        {
            var libraryTypes = assembly
                .GetTypes()
                .Where(x => HasAttribute(x.CustomAttributes, IncludeInStandardLibraryAttribute));

            var methods = new List<LibraryMethodMetadata>();

            foreach (var type in libraryTypes)
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    var libraryMethod = FindAttribute(method.CustomAttributes, JoltLibraryMethodAttribute);

                    if (libraryMethod is null)
                    {
                        continue;
                    }

                    var arguments = libraryMethod.ConstructorArguments;

                    methods.Add(new LibraryMethodMetadata
                    {
                        Name = (string)arguments[0].Value!,
                        IsValueGenerator = arguments.Count > 1 && (bool)arguments[1].Value!,
                        IsUnsafe = arguments.Count > 2 && (bool)arguments[2].Value!,
                        ReturnType = ReadReturnType(method),
                        ValidOn = ReadValidOn(method),
                        Parameters = ReadParameters(method)
                    });
                }
            }

            return methods.OrderBy(x => x.Name, StringComparer.Ordinal).ToList();
        }

        private static List<string> ReadValidOn(MethodInfo method)
        {
            var validOn = FindAttribute(method.CustomAttributes, MethodIsValidOnAttribute);

            if (validOn is null)
            {
                return new List<string>();
            }

            var targetType = validOn.ConstructorArguments[0].ArgumentType;
            var targetValue = Convert.ToInt32(validOn.ConstructorArguments[0].Value);

            return Enum
                .GetValues(targetType)
                .Cast<object>()
                .Select(x => (Name: Enum.GetName(targetType, x)!, Value: Convert.ToInt32(x)))
                .Where(x => x.Value != 0 && (targetValue & x.Value) == x.Value)
                .Select(x => char.ToLowerInvariant(x.Name[0]) + x.Name.Substring(1))
                .ToList();
        }

        private static List<LibraryParameterMetadata> ReadParameters(MethodInfo method)
        {
            return method
                .GetParameters()
                .Where(x => x.ParameterType.FullName != EvaluationContextType)
                .Select(x => new LibraryParameterMetadata
                {
                    Name = x.Name!,
                    Kind = ToParameterKind(x),
                    IsOptional = HasAttribute(x.CustomAttributes, OptionalParameterAttribute),
                    IsVariadic = HasAttribute(x.CustomAttributes, VariadicEvaluationAttribute),
                    IsLazy = HasAttribute(x.CustomAttributes, LazyEvaluationAttribute)
                })
                .ToList();
        }

        private static string ToParameterKind(ParameterInfo parameter)
        {
            var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;

            return type.Name switch
            {
                "LambdaMethod" => "lambda",
                "Enumeration" => "enumeration",
                "VariableAlias" => "variableAlias",
                "RangeVariable" => "rangeVariable",
                "DereferencedPath" => "variablePath",
                "Range" => "range",
                "Expression" => "expression",
                "IJsonObject" => "object",
                "String" => "string",
                _ => "value"
            };
        }

        private static string ReadReturnType(MethodInfo method)
        {
            var nullability = new NullabilityInfoContext().Create(method.ReturnParameter);

            return ToTypeName(method.ReturnType, nullability);
        }

        private static string ToTypeName(Type type, NullabilityInfo? nullability)
        {
            var underlyingType = Nullable.GetUnderlyingType(type);

            if (underlyingType != null)
            {
                return ToTypeName(underlyingType, null) + "?";
            }

            string name;

            if (type.IsArray)
            {
                name = ToTypeName(type.GetElementType()!, nullability?.ElementType) + "[]";
            }
            else if (type.IsGenericType)
            {
                var typeArguments = type.GetGenericArguments();
                var argumentNames = typeArguments.Select((x, i) =>
                {
                    var argumentNullability = nullability != null && i < nullability.GenericTypeArguments.Length ? nullability.GenericTypeArguments[i] : null;
                    return ToTypeName(x, argumentNullability);
                });

                name = $"{type.Name.Substring(0, type.Name.IndexOf('`'))}<{string.Join(", ", argumentNames)}>";
            }
            else
            {
                name = ToKeywordName(type) ?? type.Name;
            }

            var isNullableReference = !type.IsValueType && nullability?.ReadState == NullabilityState.Nullable;

            return isNullableReference ? name + "?" : name;
        }

        private static string? ToKeywordName(Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => "bool",
                TypeCode.Byte => "byte",
                TypeCode.SByte => "sbyte",
                TypeCode.Char => "char",
                TypeCode.Int16 => "short",
                TypeCode.UInt16 => "ushort",
                TypeCode.Int32 => "int",
                TypeCode.UInt32 => "uint",
                TypeCode.Int64 => "long",
                TypeCode.UInt64 => "ulong",
                TypeCode.Single => "float",
                TypeCode.Double => "double",
                TypeCode.Decimal => "decimal",
                TypeCode.String => "string",
                _ when type == typeof(object) => "object",
                _ when type == typeof(void) => "void",
                _ => null
            };
        }

        private static bool HasAttribute(IEnumerable<CustomAttributeData> attributes, string fullName) => FindAttribute(attributes, fullName) != null;

        private static CustomAttributeData? FindAttribute(IEnumerable<CustomAttributeData> attributes, string fullName) =>
            attributes.FirstOrDefault(x => x.AttributeType.FullName == fullName);
    }
}
