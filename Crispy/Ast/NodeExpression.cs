using System.Linq.Expressions;

namespace Crispy.Ast
{
    abstract class NodeExpression
    {
        internal protected abstract Expression Eval(Context context);

        internal protected virtual Expression SetVariable(Context context, Expression right)
        {
            throw new System.InvalidOperationException("Assignment to non-lvalue");
        }

        public virtual bool IsMember {
            get { return false; }
        }

        public virtual string Name {
            get { return ""; }
        }
    }
}
