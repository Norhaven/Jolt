using Jolt.Exceptions;
using Jolt.Library;
using Jolt.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Evaluation
{
    public sealed class ReferenceResolver : IReferenceResolver
    {
        private readonly MethodSignature[] _standardMethods;
        private readonly MethodSignature[] _thirdPartyMethods;
        private readonly ILookup<string, MethodSignature> _availableMethods;

        public ReferenceResolver(IEnumerable<MethodSignature>? thirdPartyMethods = null)
        {
            _standardMethods = Registrar.GetStandardLibraryRegistrations().ToArray();
            _thirdPartyMethods = thirdPartyMethods?.ToArray() ?? Array.Empty<MethodSignature>();
            _availableMethods = _standardMethods.Concat(_thirdPartyMethods).ToLookup(x => x.Alias);
        }

        public ReferenceResolver(MethodSignature[] standardMethods, MethodSignature[] thirdPartyMethods)
        {
            _standardMethods = standardMethods;
            _thirdPartyMethods = thirdPartyMethods;
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

            throw new JoltExecutionException($"Encountered multiple non-system methods with the name or alias '{methodName}'");
        }
    }
}
