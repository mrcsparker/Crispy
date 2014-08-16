using System.Linq.Expressions;
using Crispy.Helpers;

namespace Crispy.Ast
{
    class IndexExpression : NodeExpression
    {
        private readonly NodeExpression _target;
        private readonly NodeExpression _index;

        public IndexExpression(NodeExpression target, NodeExpression index)
        {
            _target = target;
            _index = index;
        }

        protected internal override Expression Eval(Context scope)
        {
            return Expression.Call(
                typeof(RuntimeHelpers).GetMethod("GetItem"),
                Expression.Convert(_target.Eval(scope), typeof(object)),
                Expression.Convert(_index.Eval(scope), typeof(object))
            );

        }

        internal protected override Expression SetVariable(Context scope, Expression right)
        {
            return Expression.Call(
                typeof(RuntimeHelpers).GetMethod("SetItem"),
                Expression.Convert(_target.Eval(scope), typeof(object)),
                Expression.Convert(_index.Eval(scope), typeof(object)),
                Expression.Convert(right, typeof(object))
            );
        }

        public NodeExpression Target
        {
            get { return _target; }
        }

        public NodeExpression Index
        {
            get { return _index; }
        }
    }
}
