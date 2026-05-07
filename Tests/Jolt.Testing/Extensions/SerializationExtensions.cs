using Jolt.Structure;
using Jolt.Testing.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Jolt.Testing.Extensions
{
    internal static class SerializationExtensions
    {
        private static void WriteMessage(this IMessageSink messages, string message)
        {
            messages?.OnMessage(new DiagnosticMessage(message));
        }

        public static void DeserializeInto<T>(this IXunitSerializationInfo info, T instance, IMessageSink messages) where T : TestData
        {
            messages.WriteMessage($"[DESERIALIZER] Deserializing instance of type '{typeof(T).FullName}'");

            var testContextProperty = typeof(T).GetProperty(nameof(TestData.TestContext));
            var typeName = info.GetValue<string>(testContextProperty.Name);
            var type = Type.GetType(typeName);

            testContextProperty.SetValue(instance, Activator.CreateInstance(type));

            var testTypeProperty = typeof(T).GetProperty(nameof(TestData.TestType));
            var testTypeValue = info.GetValue<string>(testTypeProperty.Name);
            testTypeProperty.SetValue(instance, Enum.Parse(typeof(TestType), testTypeValue));

            var context = instance.TestContext.CreateJsonContext(instance.TestType);

            var serializabledProperties = typeof(T).GetProperties();

            foreach (var property in serializabledProperties)
            {
                if (property.PropertyType == typeof(ITestContext) || property.PropertyType == typeof(TestType))
                {
                    continue;
                }

                if (property.GetSetMethod() is null)
                {
                    continue;
                }

                if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    messages.WriteMessage($"[DESERIALIZER] Deserializing dictionary property '{property.Name}'");

                    var serializedString = info.GetValue<string>(property.Name);

                    if (serializedString is null)
                    {
                        messages.WriteMessage($"[DESERIALIZER] Dictionary property '{property.Name}' is null");
                        continue;
                    }

                    var dictionaryValue = context.JsonTokenReader.Read(serializedString).AsObject().ToTypeOf(property.PropertyType);
                    property.SetValue(instance, dictionaryValue);
                    continue;
                }

                if (property.PropertyType == typeof(string))
                {
                    messages.WriteMessage($"[DESERIALIZER] Deserializing string property '{property.Name}' with value '{info.GetValue<string>(property.Name)}'");

                    var value = info.GetValue<string>(property.Name);
                    property.SetValue(instance, value);
                    continue;
                }

                if (property.PropertyType == typeof(int))
                {
                    messages.WriteMessage($"[DESERIALIZER] Deserializing int property '{property.Name}' with value '{info.GetValue<int>(property.Name)}'");

                    var value = info.GetValue<int>(property.Name);
                    property.SetValue(instance, value);
                    continue;
                }

                if (property.PropertyType.IsEnum)
                {
                    messages.WriteMessage($"[DESERIALIZER] Deserializing enum property '{property.Name}' with value '{info.GetValue<string>(property.Name)}'");

                    var value = info.GetValue<string>(property.Name);
                    var enumValue = Enum.Parse(property.PropertyType, value);
                    property.SetValue(instance, enumValue);
                    continue;
                }

                if (property.PropertyType == typeof(Guid))
                {
                    messages.WriteMessage($"[DESERIALIZER] Deserializing GUID property '{property.Name}' with value '{info.GetValue<string>(property.Name)}'");

                    var value = info.GetValue<string>(property.Name);
                    var guidValue = Guid.Parse(value);
                    property.SetValue(instance, guidValue);
                    continue;
                }
                             
                if (typeof(IJsonObject).IsAssignableFrom(property.PropertyType))
                {
                    messages.WriteMessage($"[DESERIALIZER] Deserializing IJsonObject property '{property.Name}' of type '{property.PropertyType.FullName}'");

                    var serializableInstance = info.GetValue<string>(property.Name);

                    if (serializableInstance is null)
                    {
                        messages.WriteMessage($"[DESERIALIZER] IJsonObject property '{property.Name}' is null");
                        continue;
                    }

                    var objectInstance = context.JsonTokenReader.Read(serializableInstance).AsObject().ToTypeOf(property.PropertyType);
                    property.SetValue(instance, objectInstance);
                    continue;
                }

                if (property.PropertyType.IsClass && typeof(IXunitSerializable).IsAssignableFrom(property.PropertyType))
                {
                    messages.WriteMessage($"[DESERIALIZER] Deserializing IXunitSerializable property '{property.Name}' of type '{property.PropertyType.FullName}'");

                    var serializableInstance = info.GetValue<IXunitSerializable>(property.Name);

                    if (serializableInstance != null)
                    {
                        property.SetValue(instance, serializableInstance);
                    }

                    continue;
                }

                if (property.PropertyType == typeof(IMessageSink))
                {
                    messages.WriteMessage($"[DESERIALIZER] Ignoring IMessageSink property '{property.Name}', instance is missing");
                    continue;
                }

                if (property.PropertyType == typeof(Type))
                {
                    messages.WriteMessage($"[DESERIALIZER] Deserializing Type type for property '{property.Name}'");
                    var typeQualifiedName = info.GetValue<string>(property.Name);

                    if (!string.IsNullOrWhiteSpace(typeQualifiedName))
                    {
                        property.SetValue(instance, Type.GetType(typeQualifiedName));
                    }

                    continue;
                }

                messages.WriteMessage($"[DESERIALIZER] Unsupported property type found during deserialization: '{property.Name}' of type '{property.PropertyType.FullName}'");

                throw new InvalidOperationException($"Unsupported property type found during deserialization: {property.Name} - {property.PropertyType.FullName}");
            }
        }

        public static void SerializeFrom<T>(this IXunitSerializationInfo info, T instance, IMessageSink messages) where T : TestData
        {
            messages.WriteMessage($"[SERIALIZER] Serializing instance of type '{typeof(T).FullName}' with test context '{instance.TestContext.GetType().FullName}' and test type '{instance.TestType}'");

            var context = instance.TestContext.CreateJsonContext(instance.TestType);
            var serializabledProperties = typeof(T).GetProperties();

            foreach (var property in serializabledProperties)
            {
                if (property.GetSetMethod() is null)
                {
                    messages.WriteMessage($"[SERIALIZER] Skipping property '{property.Name}' because it does not have a setter");
                    continue;
                }

                var value = property.GetValue(instance);

                if (value is null)
                {
                    messages.WriteMessage($"[SERIALIZER] Property '{property.Name}' is null");
                    continue;
                }

                if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    messages.WriteMessage($"[SERIALIZER] Serializing dictionary property '{property.Name}'");
                    var serializedString = context.JsonTokenReader.CreateTokenFrom(value).AsObject().ToTypeOf<string>();
                    info.AddValue(property.Name, serializedString);
                    continue;
                }

                if (property.PropertyType == typeof(string) || property.PropertyType.IsEnum)
                {
                    messages.WriteMessage($"[SERIALIZER] Serializing simple property '{property.Name}' of type '{property.PropertyType.FullName}' with value '{value}'");
                    info.AddValue(property.Name, value.ToString());
                    continue;
                }

                if (property.PropertyType == typeof(int))
                {
                    messages.WriteMessage($"[SERIALIZER] Serializing simple property '{property.Name}' of type '{property.PropertyType.FullName}' with value '{value}'");
                    info.AddValue(property.Name, value);
                    continue;
                }

                if (property.PropertyType == typeof(Guid))
                {
                    messages.WriteMessage($"[SERIALIZER] Serializing simple property '{property.Name}' of type '{property.PropertyType.FullName}' with value '{value}'");
                    info.AddValue(property.Name, value.ToString());
                    continue;
                }

                if (property.PropertyType.IsInterface && typeof(ITestContext).IsAssignableFrom(property.PropertyType))
                {
                    messages.WriteMessage($"[SERIALIZER] Serializing test context property '{property.Name}' of type '{property.PropertyType.FullName}'");
                    info.AddValue(property.Name, property.GetValue(instance).GetType().AssemblyQualifiedName);
                    continue;
                }
                
                if (typeof(IXunitSerializable).IsAssignableFrom(property.PropertyType))
                {
                    messages.WriteMessage($"[SERIALIZER] Serializing IXunitSerializable property '{property.Name}' of type '{property.PropertyType.FullName}'");
                    info.AddValue(property.Name, value);
                    continue;
                }

                if (typeof(IMessageSink).IsAssignableFrom(property.PropertyType))
                {
                    messages.WriteMessage($"[SERIALIZER] Skipping IMessageSink property '{property.Name}' of type '{property.PropertyType.FullName}'");
                    continue;
                }

                if (typeof(IJsonObject).IsAssignableFrom(property.PropertyType))
                {
                    messages.WriteMessage($"[SERIALIZER] Serializing IJsonObject property '{property.Name}' of type '{property.PropertyType.FullName}'");
                    var serializedString = ((IJsonObject)property.GetValue(instance)).ToTypeOf<string>();
                    info.AddValue(property.Name, serializedString);
                    continue;
                }

                if (property.PropertyType == typeof(Type))
                {
                    messages.WriteMessage($"[SERIALIZER] Serializing Type type for property '{property.Name}'");
                    info.AddValue(property.Name, ((Type)property.GetValue(instance)).AssemblyQualifiedName);
                    continue;
                }

                messages.WriteMessage($"[SERIALIZER] Unsupported property type found during serialization: '{property.Name}' of type '{property.PropertyType.FullName}'");

                throw new InvalidOperationException($"Unsupported property type found during serialization: {property.Name} - {property.PropertyType.FullName}");
            }
        }

        public static Dictionary<string, string> GetValueAsDictionary(this IXunitSerializationInfo info, IJsonContext context, string name)
        {
            var value = info.GetValue<string>(name);
            return context.JsonTokenReader.CreateTokenFrom(value).AsObject().ToTypeOf<Dictionary<string, string>>();
        }

        public static T GetValue<T>(this IXunitSerializationInfo info, IJsonContext context, string name)
        {
            var value = info.GetValue<string>(name);
            return context.JsonTokenReader.CreateTokenFrom(value).AsObject().ToTypeOf<T>();
        }

        public static void AddValueAsDictionary(this IXunitSerializationInfo info, IJsonContext context, string name, Dictionary<string, string> data)
        {
            var value = context.JsonTokenReader.CreateTokenFrom(data).AsObject().ToTypeOf<string>();
            info.AddValue(name, value);
        }

        public static void AddValue<T>(this IXunitSerializationInfo info, string name, T value)
        {
            info.AddValue(name, value);
        }
    }
}
