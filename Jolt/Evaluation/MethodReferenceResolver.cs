﻿using Jolt.Exceptions;
using Jolt.Library;
using Jolt.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Evaluation
{
    public sealed class MethodReferenceResolver : IMethodReferenceResolver
    {
        private readonly MethodSignature[] _standardMethods;
        private readonly List<MethodSignature> _thirdPartyMethods = new List<MethodSignature>();
        private readonly IMessageProvider _messageProvider;
        private ILookup<string, MethodSignature> _availableMethods;

        public MethodReferenceResolver(IMessageProvider messageProvider)
        {
            _standardMethods = Registrar.GetStandardLibraryRegistrations().ToArray();
            _availableMethods = _standardMethods.Concat(_thirdPartyMethods).ToLookup(x => x.Alias);
            _messageProvider = messageProvider;
        }

        public void Clear()
        {
            _thirdPartyMethods.Clear();
            _availableMethods = _standardMethods.ToLookup(x => x.Alias);
        }

        public void RegisterMethods(IEnumerable<MethodRegistration> methodRegistrations, object? methodContext = default)
        {
            if (methodRegistrations is null)
            {
                return;
            }

            var methods = Registrar.GetExternalMethodRegistrations(methodRegistrations?.ToArray() ?? Array.Empty<MethodRegistration>(), _messageProvider, methodContext).ToArray();

            _thirdPartyMethods.AddRange(methods);
            _availableMethods = _standardMethods.Concat(_thirdPartyMethods).ToLookup(x => x.Alias);
        }

        public MethodSignature? GetMethod(string methodName)
        {
            var matchingMethods = _availableMethods[methodName].ToArray();

            if (!matchingMethods.Any())
            {
                return default;
            }

            if (matchingMethods.Length == 1)
            {
                return matchingMethods[0];
            }

            var systemMethod = matchingMethods.FirstOrDefault(x => x.IsSystemMethod);

            if (systemMethod != null)
            {
                return systemMethod;
            }

            throw _messageProvider.CreateErrorFor<MethodReferenceResolver>(MessageCategory.Execution, ExceptionCode.EncounteredMultipleNonSystemMethodsWithSameNameOrAlias, methodName);
        }
    }
}
