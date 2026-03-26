using System.Linq.Expressions;

namespace Crispy.Ast
{
    sealed class ExpressionStatement : NodeExpression
    {
        public NodeExpression Expr { get; }

        public ExpressionStatement(NodeExpression expression)
        {
            Expr = expression;
        }

        protected internal override Expression Eval(Context scope)
        {
            Expression expression = Expr.Eval(scope);
            return expression;
        }
    }
}
