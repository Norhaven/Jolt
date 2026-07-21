using Jolt.Evaluation;
using Jolt.Expressions;
using Jolt.Extensions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Library.StandardLibrary
{
    internal sealed class QueryMethods : EnumerableWithLambda
    {
        public QueryMethods(IEnumerable<IJsonToken> sequence, LambdaMethod lambda) 
            : base(sequence, lambda)
        {
        }

        public QueryMethods(LambdaMethod lambda)
            : base(Enumerable.Empty<IJsonToken>(), lambda)
        {
        }

        public QueryMethods(IEnumerable<IJsonToken> sequence)
            : base(sequence, null)
        {
        }

        public IEnumerable<IJsonToken> Select(EvaluationContext context)
        {
            return Project(context);
        }

        public IEnumerable<IJsonToken> Where(EvaluationContext context)
        {
            return ProjectWhere(context);
        }

        public IJsonToken Any(EvaluationContext context)
        {
            var isAny = Lambda switch
            {
                null => Sequence.Any(),
                _ => Where(context).Any()
            };
            
            return context.CreateTokenFrom(isAny);
        }

        public IEnumerable<IJsonToken> Zip(IJsonArray secondSequence, EvaluationContext context)
        {
            return ProjectZip(secondSequence, context);
        }

        public IEnumerable<IJsonToken> Distinct(EvaluationContext context)
        {
            return ProjectByUniqueKey(new HashSet<IJsonToken>(), context);
        }

        public IJsonToken ExecuteLambda(IJsonToken parameterValue, EvaluationContext context)
        {
            var result = ExecuteLambdaWith(parameterValue, context, (body, ctx) => QueryResult<IJsonToken>.Some(ExecuteLambdaBody(body, ctx)));

            return result.Result;
        }

        public IJsonToken ExecuteLambda(IJsonToken parameterValue, IJsonToken secondParameterValue, EvaluationContext context)
        {
            var result = ExecuteLambdaWith(parameterValue, secondParameterValue, context, (body, ctx) =>
            {
                return QueryResult<IJsonToken>.Some(ExecuteLambdaBody(body, ctx));
            });

            return result.Result;
        }

        public new IEnumerable<IJsonToken> TakeWhile(EvaluationContext context)
        {
            return base.TakeWhile(context);
        }

        public new IEnumerable<IJsonToken> Take(EvaluationContext context, int count)
        {
            return base.Take(context, count);
        }

        public new IEnumerable<IJsonToken> SkipWhile(EvaluationContext context)
        {
            return base.SkipWhile(context);
        }

        public new IEnumerable<IJsonToken> Skip(EvaluationContext context, int count)
        {
            return base.Skip(context, count);
        }
    }
}
