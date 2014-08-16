using System.Linq.Expressions;
using Crispy.Helpers;

namespace Crispy.Ast
{
    class ReturnStatement : NodeExpression
    {
        private readonly NodeExpression _returnExpression;

        public ReturnStatement()
        {
        }

        public ReturnStatement(NodeExpression returnExpression)
        {
            _returnExpression = returnExpression;
        }

        protected internal override Expression Eval(Context scope)
        {
            scope.ReturnLabel = Expression.Label(typeof(object));

            if (_returnExpression != null)
            {
                var ret = Expression.Return(scope.ReturnLabel, RuntimeHelpers.EnsureObjectResult(_returnExpression.Eval(scope)), typeof(object));
                return ret;
            }
            return Expression.Return(scope.ReturnLabel, Expression.Constant(true));
        }
    }
}
