using System.Linq.Expressions;

namespace Crispy.Ast
{
    class ExpressionStatement : NodeExpression
    {
        private readonly NodeExpression _expression;

        public ExpressionStatement(NodeExpression expression)
        {
            _expression = expression;
        }

        protected internal override Expression Eval(Context scope)
        {
            Expression expression = _expression.Eval(scope);
            return expression;
        }

        public NodeExpression Expr
        {
            get { return _expression; }
        }
    }
}
