using Jolt.Evaluation;
using Jolt.Exceptions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Library
{
    public static class StandardMethods
    {
        [JoltLibraryMethod("valueOf")]
        public static IJsonToken? ValueOf(string path, EvaluationContext context)
        {
            // ValueOf will always start a search from the root, other similar methods may search differently.

            return context.Context.QueryPathProvider.SelectNodeAtPath(context.ClosureSources, path, JsonQueryMode.StartFromRoot);
        }

        [JoltLibraryMethod("loop")]
        public static IEnumerable<IJsonToken> LoopOnArrayAtPath(string path, EvaluationContext context)
        {
            if (context.Token.CurrentTransformerToken.Type != JsonTokenType.Array)
            {
                yield break;
            }

            var closestViableNode = context.Context.QueryPathProvider.SelectNodeAtPath(context.ClosureSources, path, JsonQueryMode.StartFromClosestMatch);

            if (closestViableNode?.Type != JsonTokenType.Array)
            {
                yield break;
            }

            var transformerArray = context.Token.CurrentTransformerToken.AsArray();

            var contentTemplate = transformerArray.RemoveAt(0);

            foreach (var propertyOrElement in closestViableNode.AsArray() ?? Enumerable.Empty<IJsonToken>())
            {
                var templateCopy = contentTemplate.Copy();
                var propertyName = context.Token.ResolvedPropertyName ?? context.Token.PropertyName;

                var templateEvaluationToken = new EvaluationToken(propertyName, default, transformerArray, templateCopy);

                context.ClosureSources.Push(propertyOrElement);

                yield return context.Transform(templateEvaluationToken, context.ClosureSources);
                
                context.ClosureSources.Pop();
            }
        }

        [JoltLibraryMethod("loopValueOf")]
        public static IJsonToken LoopValueOf(string path, EvaluationContext context)
        {
            // LoopValueOf will always search from the closest node, other similar methods may search differently.

            return context.Context.QueryPathProvider.SelectNodeAtPath(context.ClosureSources, path, JsonQueryMode.StartFromClosestMatch);
        }

        [JoltLibraryMethod("loopValue")]
        public static IJsonToken? LoopValue(EvaluationContext context)
        {
            return context.ClosureSources.Peek();
        }
    }
}
