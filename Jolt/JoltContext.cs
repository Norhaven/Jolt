﻿using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Library;
using Jolt.Parsing;
using Jolt.Structure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Jolt
{
    public class JoltContext : IJsonContext
    {
        public string JsonTransformer { get; private set; }

        public IExpressionParser ExpressionParser { get; }

        public IExpressionEvaluator ExpressionEvaluator { get; }

        public ITokenReader TokenReader { get; }

        public IJsonTokenReader JsonTokenReader { get; }

        public IQueryPathProvider QueryPathProvider { get; }

        public MethodRegistration[] MethodRegistrations { get; private set; } = Array.Empty<MethodRegistration>();

        public IMethodReferenceResolver ReferenceResolver { get; }

        public IMessageProvider MessageProvider { get; }

        public IErrorHandler ErrorHandler { get; }

        public object? MethodContext { get; private set; }

        public JoltContext(string jsonTransformer,
                           IExpressionParser expressionParser,
                           IExpressionEvaluator expressionEvaluator,
                           ITokenReader tokenReader,
                           IJsonTokenReader jsonTokenReader,
                           IQueryPathProvider queryPathProvider,
                           IMethodReferenceResolver referenceResolver,
                           IMessageProvider messageProvider,
                           IErrorHandler errorHandler,
                           object? methodContext = default)
        {
            JsonTransformer = jsonTransformer;
            ExpressionParser = expressionParser;
            ExpressionEvaluator = expressionEvaluator;
            TokenReader = tokenReader;
            JsonTokenReader = jsonTokenReader;
            QueryPathProvider = queryPathProvider;
            ReferenceResolver = referenceResolver;
            MessageProvider = messageProvider;
            ErrorHandler = errorHandler;
            MethodContext = methodContext;
        }

        public IJsonContext RegisterMethod(MethodRegistration method)
        {
            if (method is null)
            {
                return this;
            }

            MethodRegistrations = MethodRegistrations.Concat(new[] { method }).ToArray();

            return this;
        }

        public IJsonContext RegisterAllMethods(IEnumerable<MethodRegistration> methods)
        {
            if (methods is null)
            {
                return this;
            }

            MethodRegistrations = MethodRegistrations.Concat(methods).ToArray();

            return this;
        }

        public IJsonContext UseTransformer(string jsonTransformer)
        {
            JsonTransformer = jsonTransformer;

            return this;
        }

        public IJsonContext UseMethodContext(object? methodContext)
        {
            MethodContext = methodContext;

            return this;
        }

        public IJsonContext RegisterAllMethodsFrom<T>()
        {
            var type = typeof(T);

            var methods = from method in type.GetMethods(BindingFlags.Public)
                          let attribute = method.GetCustomAttribute<JoltExternalMethodAttribute>()
                          where attribute != null
                          select method.IsStatic ? new MethodRegistration(type.AssemblyQualifiedName, method.Name) : new MethodRegistration(method.Name, attribute.Name);

            MethodRegistrations = MethodRegistrations.Concat(methods).ToArray();

            return this;
        }
    }
}
