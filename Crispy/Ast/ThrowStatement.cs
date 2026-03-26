using System;
using System.Linq.Expressions;
using System.Reflection;
using Crispy.Helpers;

namespace Crispy.Ast
{
    sealed class ThrowStatement : NodeExpression
    {
        private static readonly MethodInfo CoerceToExceptionMethod =
            typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CoerceToException)) ??
            throw new InvalidOperationException("RuntimeHelpers.CoerceToException was not found.");

        private readonly NodeExpression? _throwValue;

        public ThrowStatement()
        {
        }

        public ThrowStatement(NodeExpression throwValue)
        {
            _throwValue = throwValue;
        }

        protected internal override Expression Eval(Context scope)
        {
            return _throwValue != null
                ? Expression.Throw(
                    Expression.Call(
                        CoerceToExceptionMethod,
                        Expression.Convert(_throwValue.Eval(scope), typeof(object))),
                    typeof(object))
                : Expression.Throw(
                    scope.ActiveCaughtException ??
                    throw new InvalidOperationException("Call to Throw without value not inside catch."),
                    typeof(object));
        }
    }
}
