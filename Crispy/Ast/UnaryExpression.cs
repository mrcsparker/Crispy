using System.Linq.Expressions;

namespace Crispy.Ast
{
    sealed class UnaryExpression : NodeExpression
    {
        private readonly ExpressionType _unaryOperator;
        private readonly NodeExpression _expression;

        public UnaryExpression(ExpressionType unaryOperator, NodeExpression expression)
        {
            _unaryOperator = unaryOperator;
            _expression = expression;
        }

        protected internal override Expression Eval(Context scope)
        {
            return Expression.Dynamic(
                scope.Runtime.GetUnaryOperationBinder(_unaryOperator),
                typeof(object),
                _expression.Eval(scope));
        }
    }
}
