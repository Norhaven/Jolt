using Jolt.Testing.Extensions;
using Jolt.Testing.Json.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Jolt.Testing.Unit
{
    public abstract class SerializationTests
    {
        protected sealed class AttributeInfo : LongLivedMarshalByRefObject, IAttributeInfo
        {   
            private readonly JsonTestAttribute _attribute;

            public AttributeInfo(JsonTestAttribute attribute)
            {
                _attribute = attribute;
            }

            public string Name => "ExampleAttribute";
            public ITypeInfo Attribute => throw new NotImplementedException();

            public IEnumerable<object> GetConstructorArguments()
            {
                return Array.Empty<object>();
            }

            public IEnumerable<IAttributeInfo> GetCustomAttributes(string assemblyQualifiedAttributeTypeName)
            {
                return Array.Empty<IAttributeInfo>();
            }

            public TValue GetNamedArgument<TValue>(string argumentName)
            {
                switch (argumentName)
                {
                    case nameof(JsonTestAttribute.TestJsonFile): return (TValue)(object)_attribute.TestJsonFile;
                    case nameof(JsonTestAttribute.TestContext): return (TValue)(object)_attribute.TestContext;
                    default: return default;
                };
            }
        }

        protected sealed class TestMethod : LongLivedMarshalByRefObject, ITestMethod
        {
            private sealed class MethodInfo : LongLivedMarshalByRefObject, IMethodInfo
            {
                public string Name => "Example";
                public ITypeInfo Type => throw new NotImplementedException();
                public IMethodInfo Method => this;

                public bool IsAbstract => false;

                public bool IsGenericMethodDefinition => false;

                public bool IsPublic => true;

                public bool IsStatic => true;

                public ITypeInfo ReturnType => throw new NotImplementedException();

                public IParameterInfo[] GetParameters() => Array.Empty<IParameterInfo>();

                public object[] GetCustomAttributes(bool inherit)
                {
                    return Array.Empty<object>();
                }
                public object[] GetCustomAttributes(Type attributeType, bool inherit)
                {
                    return Array.Empty<object>();
                }
                public bool HasAttribute(Type attributeType, bool inherit)
                {
                    return false;
                }

                public IEnumerable<IAttributeInfo> GetCustomAttributes(string assemblyQualifiedAttributeTypeName)
                {
                    return Array.Empty<IAttributeInfo>();
                }

                public IEnumerable<ITypeInfo> GetGenericArguments()
                {
                    return Array.Empty<ITypeInfo>();
                }

                IEnumerable<IParameterInfo> IMethodInfo.GetParameters()
                {
                    return GetParameters();
                }

                public IMethodInfo MakeGenericMethod(params ITypeInfo[] typeArguments)
                {
                    throw new NotImplementedException();
                }
            }

            private sealed class TypeInfo : LongLivedMarshalByRefObject, ITypeInfo
            {
                public string Name => "Example";
                public IAssemblyInfo Assembly => throw new NotImplementedException();
                public ITypeInfo BaseType => throw new NotImplementedException();
                public bool IsAbstract => false;
                public bool IsGenericTypeDefinition => false;
                public bool IsPublic => true;
                public bool IsSealed => true;
                public bool IsStatic => true;

                public IEnumerable<ITypeInfo> Interfaces => throw new NotImplementedException();

                public bool IsGenericParameter => throw new NotImplementedException();

                public bool IsGenericType => throw new NotImplementedException();

                public bool IsValueType => throw new NotImplementedException();

                public IEnumerable<IAttributeInfo> GetCustomAttributes(string assemblyQualifiedAttributeTypeName)
                {
                    throw new NotImplementedException();
                }
                public IEnumerable<ITypeInfo> GetGenericArguments()
                {
                    throw new NotImplementedException();
                }

                public IMethodInfo GetMethod(string methodName, bool includePrivateMethod)
                {
                    throw new NotImplementedException();
                }

                public IEnumerable<IMethodInfo> GetMethods(bool includePrivateMethods)
                {
                    throw new NotImplementedException();
                }
            }

            private sealed class ExampleClass : LongLivedMarshalByRefObject, ITestClass
            {
                public string Name => "Example";
                public ITypeInfo Class => new TypeInfo();

                public ITestCollection TestCollection => throw new NotImplementedException();

                public void Deserialize(IXunitSerializationInfo info)
                {
                    throw new NotImplementedException();
                }

                public IEnumerable<IAttributeInfo> GetCustomAttributes(string assemblyQualifiedAttributeTypeName)
                {
                    throw new NotImplementedException();
                }

                public void Serialize(IXunitSerializationInfo info)
                {
                    throw new NotImplementedException();
                }
            }

            public IMethodInfo Method => new MethodInfo();

            public ITestClass TestClass => new ExampleClass();

            public void Deserialize(IXunitSerializationInfo info)
            {
                
            }

            public void Serialize(IXunitSerializationInfo info)
            {
                
            }
        }

        protected sealed class DiscoveryOptions : ITestFrameworkDiscoveryOptions
        {
            public TValue GetValue<TValue>(string name)
            {
                return default;
            }

            public void SetValue<TValue>(string name, TValue value)
            {
                
            }
        }

        protected sealed class TestMessageSink : LongLivedMarshalByRefObject, IMessageSink
        {
            public bool OnMessage(IMessageSinkMessage message)
            {
                Trace.WriteLine($"[MESSAGE SINK] {message.GetType().Name}: {message.ToString()}");
                return true;
            }
        }

        protected sealed class SerializationInfo : IXunitSerializationInfo
        {
            private readonly Dictionary<string, (object Value, Type Type)> _values = new Dictionary<string, (object Value, Type Type)>();

            public void AddValue(string key, object value, Type type = null)
            {
                if (typeof(IXunitSerializable).IsAssignableFrom(value?.GetType()))
                {
                    var info = new SerializationInfo();

                    ((IXunitSerializable)value).Serialize(info);

                    _values.Add(key, (info, value.GetType()));
                }
                else
                {
                    _values.Add(key, (value, type ?? value?.GetType() ?? typeof(object)));
                }
            }

            public object GetValue(string key, Type type)
            {
                if (_values.TryGetValue(key, out var serialized))
                {
                    if (!serialized.Type.IsInterface && serialized.Value is IXunitSerializationInfo serializationInfo)
                    {
                        var instance = Activator.CreateInstance(serialized.Type) as IXunitSerializable;
                        instance.Deserialize(serializationInfo);
                        return instance;
                    }
                }

                return _values.TryGetValue(key, out var value) && (value.Type == type || type == typeof(object))
                    ? value.Value
                    : default;
            }

            public T GetValue<T>(string key)
            {
                return (T)GetValue(key, typeof(T));
            }
        }

        protected JoltTestCase<T> SerializeAndDeserialize<T>(JoltTestCase<T> original, IMessageSink messages) where T : TestData, IXunitSerializable, new()
        {
            var serializationInfo = new SerializationInfo();
            original.Serialize(serializationInfo);
            // Create a new instance and deserialize into it
            var deserialized = Activator.CreateInstance<JoltTestCase<T>>();
            deserialized.Deserialize(serializationInfo);
            return deserialized;
        }

        protected T SerializeAndDeserialize<T>(T original, IMessageSink messages) where T : TestData, IXunitSerializable
        {
            var serializationInfo = new SerializationInfo();

            serializationInfo.SerializeFrom(original, messages);
                        
            // Create a new instance and deserialize into it
            var deserialized = Activator.CreateInstance<T>();

            serializationInfo.DeserializeInto(deserialized, messages);

            return deserialized;
        }
    }
}
