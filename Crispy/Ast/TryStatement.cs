using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Crispy.Helpers;

namespace Crispy.Ast
{
    sealed class TryStatement : NodeExpression
    {
        private static readonly MethodInfo GetCaughtValueMethod =
            typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetCaughtValue)) ??
            throw new InvalidOperationException("RuntimeHelpers.GetCaughtValue was not found.");

        private readonly NodeExpression _tryBody;
        private readonly string? _catchName;
        private readonly NodeExpression? _catchBody;
        private readonly NodeExpression? _finallyBody;

        public TryStatement(
            NodeExpression tryBody,
            string? catchName,
            NodeExpression? catchBody,
            NodeExpression? finallyBody)
        {
            _tryBody = tryBody;
            _catchName = catchName;
            _catchBody = catchBody;
            _finallyBody = finallyBody;
        }

        protected internal override Expression Eval(Context scope)
        {
            var tryBody = RuntimeHelpers.EnsureObjectResult(_tryBody.Eval(scope));
            var catchBlock = _catchBody != null ? BuildCatch(scope) : null;
            var finallyBody = _finallyBody != null ? BuildFinally(scope) : null;

            return catchBlock != null && finallyBody != null
                ? Expression.TryCatchFinally(tryBody, finallyBody, catchBlock)
                : catchBlock != null
                ? Expression.TryCatch(tryBody, catchBlock)
                : Expression.TryFinally(
                    tryBody,
                    finallyBody ?? throw new InvalidOperationException("finally body was not available."));
        }

        private CatchBlock BuildCatch(Context scope)
        {
            var caughtException = Expression.Parameter(typeof(Exception), "__caughtException");
            var catchScope = new Context(scope, "catch")
            {
                CaughtException = caughtException
            };

            var expressions = new List<Expression>();
            if (_catchName != null)
            {
                var caughtValue = catchScope.GetOrMakeLocal(_catchName);
                expressions.Add(
                    Expression.Assign(
                        caughtValue,
                        Expression.Convert(
                            Expression.Call(GetCaughtValueMethod, caughtException),
                            caughtValue.Type)));
            }

            expressions.Add(RuntimeHelpers.EnsureObjectResult(
                (_catchBody ?? throw new InvalidOperationException("catch body was not available."))
                .Eval(catchScope)));

            return Expression.Catch(
                caughtException,
                catchScope.Variables.Count > 0
                    ? Expression.Block(typeof(object), [.. catchScope.Variables.Values], expressions)
                    : Expression.Block(typeof(object), expressions));
        }

        private BlockExpression BuildFinally(Context scope)
        {
            var finallyScope = new Context(scope, "finally");
            var expressions = new List<Expression>
            {
                (_finallyBody ?? throw new InvalidOperationException("finally body was not available."))
                .Eval(finallyScope),
                Expression.Empty()
            };

            return finallyScope.Variables.Count > 0
                ? Expression.Block(typeof(void), [.. finallyScope.Variables.Values], expressions)
                : Expression.Block(typeof(void), expressions);
        }
    }
}
