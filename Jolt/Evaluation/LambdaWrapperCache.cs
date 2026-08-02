using Jolt.Structure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Jolt.Evaluation
{
    internal sealed class LambdaWrapperCache
    {
        private static readonly ConcurrentDictionary<(string MethodName, Type DelegateType), MethodInfo> _lambdaFactoryCache = new ConcurrentDictionary<(string MethodName, Type DelegateType), MethodInfo>();

        public static Delegate BuildLambdaDelegate(string receiverMethodName, Type delegateType, Func<object, IJsonToken?> executeLambda)
        {
            var invokeMethod = delegateType.GetMethod("Invoke")!;
            
            var lambdaFactory = GetOrAddLambdaFactory(receiverMethodName, delegateType, delegateType.GetGenericArguments());

            return (Delegate)lambdaFactory?.Invoke(null, new object[] { executeLambda })!;
        }

        private static MethodInfo? GetOrAddLambdaFactory(string receiverMethodName, Type delegateType, Type[] genericTypeParameters)
        {
            MethodInfo? GetTypedLambdaFactory()
            {
                var methods = typeof(LambdaWrapperCache).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Where(x => x.Name == nameof(CreateTypedLambda));
                var resolvedFactoryMethod = methods.FirstOrDefault(x => x.GetGenericArguments().Length == genericTypeParameters.Length);

                return resolvedFactoryMethod?.MakeGenericMethod(genericTypeParameters);
            }

            return _lambdaFactoryCache.GetOrAdd((receiverMethodName, delegateType), ((string name, Type type) key) => GetTypedLambdaFactory()!);
        }

        private static Func<TParam, TResult> CreateTypedLambda<TParam, TResult>(Func<object, IJsonToken?> executeLambda)
            => x => executeLambda(new object[] { x! }).ToTypeOf<TResult>();

        private static Func<TParam1, TParam2, TResult> CreateTypedLambda<TParam1, TParam2, TResult>(Func<object, IJsonToken?> executeLambda)
            => (x, y) => executeLambda(new object[] { x!, y! }).ToTypeOf<TResult>();
    }
}
