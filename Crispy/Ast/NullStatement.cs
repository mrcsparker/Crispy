using System.Linq.Expressions;

namespace Crispy.Ast
{
    internal class NullStatement : NodeExpression
    {
        private static readonly NullStatement InternalInstance = new NullStatement();

        private NullStatement()
        {
        }

        public static NullStatement Instance
        {
            get { return InternalInstance; }
        }

        protected internal override Expression Eval(Context scope)
        {
            return Expression.Constant(null);
        }
    }
}
