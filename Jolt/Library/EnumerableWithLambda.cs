using Jolt.Evaluation;
using Jolt.Expressions;
using Jolt.Extensions;
using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jolt.Library
{
    internal abstract class EnumerableWithLambda
    {
        internal sealed class QueryResult<TValue> where TValue : IJsonToken
        {
            public static QueryResult<TValue> None => new QueryResult<TValue>(default, false);
            public static QueryResult<TValue> Some(TValue result) => new QueryResult<TValue>(result, true);

            public TValue Result { get; }
            public bool HasResult { get; }

            public QueryResult(TValue result, bool hasResult)
            {
                Result = result;
                HasResult = hasResult;
            }
        }

        public IEnumerable<IJsonToken> Sequence { get; }
        public LambdaMethod Lambda { get; }

        public EnumerableWithLambda(IEnumerable<IJsonToken> sequence, LambdaMethod lambda)
        {
            Sequence = sequence;
            Lambda = lambda;
        }

        public QueryResult<IJsonToken> ExecuteLambdaWith(IJsonToken value, EvaluationContext context, Func<Expression, EvaluationContext, QueryResult<IJsonToken>> execute)
        {
            var itemToken = context.CreateTokenFrom(value);
            var loopVariable = new RangeVariable(Lambda.Variables[0].Name, itemToken);

            context.Scope.AddOrUpdateVariable(loopVariable);

            try
            {
                return execute(Lambda.Body, context);
            }
            finally
            {
                context.Scope.RemoveCurrentVariablesLayer();
            }
        }

        public QueryResult<IJsonToken> ExecuteLambdaWith(IJsonToken value, IJsonToken secondValue, EvaluationContext context, Func<Expression, EvaluationContext, QueryResult<IJsonToken>> execute)
        {
            var itemToken = context.CreateTokenFrom(value);
            var secondItemToken = context.CreateTokenFrom(secondValue);

            var variable = new RangeVariable(Lambda.Variables[0].Name, itemToken);
            var secondVariable = new RangeVariable(Lambda.Variables[1].Name, secondItemToken);

            context.Scope.AddOrUpdateVariable(variable);
            context.Scope.AddOrUpdateVariable(secondVariable);

            try
            {
                return execute(Lambda.Body, context);
            }
            finally
            {
                context.Scope.RemoveCurrentVariablesLayer();
            }
        }

        protected IEnumerable<IJsonToken> Project(EvaluationContext context)
        {
            return from item in Sequence
                   let token = ExecuteLambdaWith(item, context, (body, ctx) => QueryResult<IJsonToken>.Some(ExecuteLambdaBody(body, ctx)))
                   where token.HasResult
                   select token.Result;
        }

        protected IEnumerable<IJsonToken> ProjectWhere(EvaluationContext context)
        {
            return from item in Sequence
                   let token = ExecuteLambdaWith(item, context, (body, ctx) => ExecuteLambdaBody(body, ctx).AsValue().ToTypeOf<bool>() ? QueryResult<IJsonToken>.Some(item) : QueryResult<IJsonToken>.None)
                   where token.HasResult
                   select token.Result;
        }

        protected IEnumerable<IJsonToken> ProjectByUniqueKey(ICollection<IJsonToken> collection, EvaluationContext context)
        {
            var projectionItems = from item in Sequence
                                  let token = ExecuteLambdaWith(item, context, (body, ctx) => QueryResult<IJsonToken>.Some(ExecuteLambdaBody(body, ctx)))
                                  where token.HasResult
                                  select (Key: token.Result, Item: item);

            foreach (var (key, item) in projectionItems)
            {
                if (!collection.Contains(key))
                {
                    collection.Add(key);
                    yield return item;
                }
            }
        }

        protected IEnumerable<IJsonToken> ProjectZip(IJsonArray secondSequence, EvaluationContext context)
        {
            var enumerator = secondSequence.GetEnumerator();

            foreach (var item in Sequence)
            {
                if (!enumerator.MoveNext())
                {
                    yield break;
                }

                var secondItem = enumerator.Current;

                var zippedToken = ExecuteLambdaWith(item, secondItem, context, (body, ctx) => QueryResult<IJsonToken>.Some(ExecuteLambdaBody(body, ctx)));

                if (zippedToken.HasResult)
                {
                    yield return zippedToken.Result;
                }
            }
        }

        protected virtual IEnumerable<IJsonToken> TakeWhile(EvaluationContext context)
        {
            foreach (var item in Sequence)
            {
                var result = ExecuteLambdaWith(item, context, (body, ctx) =>
                {
                    if (ExecuteLambdaBody(body, ctx)?.AsValue().ToTypeOf<bool>() == true)
                    {
                        return QueryResult<IJsonToken>.Some(context.CreateTokenFrom(item));
                    }

                    return QueryResult<IJsonToken>.None;
                });

                if (result.HasResult)
                {
                    yield return result.Result;
                }
                else
                {
                    yield break;
                }
            }
        }

        protected virtual IEnumerable<IJsonToken> Take(EvaluationContext context, int count)
        {
            var taken = 0;

            foreach (var item in Sequence)
            {
                if (taken >= count)
                {
                    yield break;
                }

                yield return item;

                taken++;
            }
        }

        protected IEnumerable<IJsonToken> SkipWhile(EvaluationContext context)
        {
            var isSkipping = true;

            foreach (var item in Sequence)
            {
                var result = ExecuteLambdaWith(item, context, (body, ctx) =>
                {
                    if (isSkipping &&  ExecuteLambdaBody(body, ctx)?.AsValue().ToTypeOf<bool>() == true)
                    {
                        return QueryResult<IJsonToken>.None;
                    }

                    isSkipping = false;

                    return QueryResult<IJsonToken>.Some(context.CreateTokenFrom(item));
                });

                if (result.HasResult)
                {
                    yield return result.Result;
                }
            }
        }

        protected virtual IEnumerable<IJsonToken> Skip(EvaluationContext context, int count)
        {
            var skipped = 0;

            foreach (var item in Sequence)
            {
                if (skipped < count)
                {
                    skipped++;
                    continue;
                }

                yield return item;
            }
        }

        protected IJsonToken? ExecuteLambdaBody(Expression lambdaBodyExpression, EvaluationContext context)
        {
            var evaluationContext = new EvaluationContext(
                context.Mode,
                lambdaBodyExpression,
                context.JsonContext,
                context.Token,
                context.Scope,
                context.Transform);

            var result = context.JsonContext.ExpressionEvaluator.Evaluate(evaluationContext);

            return result.TransformedToken;
        }
    }
}
