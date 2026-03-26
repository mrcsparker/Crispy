using System.Linq.Expressions;

namespace Crispy.Ast
{
    internal sealed class NullStatement : NodeExpression
    {
        public static NullStatement Instance { get; } = new NullStatement();

        private NullStatement()
        {
        }

        protected internal override Expression Eval(Context scope)
        {
            return Expression.Constant(null);
        }
    }
}
