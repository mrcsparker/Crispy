using System.Linq.Expressions;
using Crispy.Helpers;

namespace Crispy.Ast
{
    sealed class ReturnStatement : NodeExpression
    {
        private readonly NodeExpression? _returnExpression;

        public ReturnStatement()
        {
        }

        public ReturnStatement(NodeExpression returnExpression)
        {
            _returnExpression = returnExpression;
        }

        protected internal override Expression Eval(Context scope)
        {
            var callableScope = scope.CallableScope;
            var returnLabel = callableScope.ReturnLabel ??= Expression.Label(typeof(object));

            if (_returnExpression != null)
            {
                var ret = Expression.Return(returnLabel, RuntimeHelpers.EnsureObjectResult(_returnExpression.Eval(scope)), typeof(object));
                return ret;
            }
            return Expression.Return(returnLabel, Expression.Constant(true));
        }
    }
}
