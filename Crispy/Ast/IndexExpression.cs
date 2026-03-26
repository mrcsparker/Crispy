using System;
using System.Linq.Expressions;
using System.Reflection;
using Crispy.Helpers;

namespace Crispy.Ast
{
    sealed class IndexExpression : NodeExpression
    {
        private static readonly MethodInfo GetItemMethod =
            typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetItem)) ??
            throw new InvalidOperationException("RuntimeHelpers.GetItem was not found.");

        private static readonly MethodInfo SetItemMethod =
            typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.SetItem)) ??
            throw new InvalidOperationException("RuntimeHelpers.SetItem was not found.");

        public NodeExpression Target { get; }
        public NodeExpression Index { get; }

        public IndexExpression(NodeExpression target, NodeExpression index)
        {
            Target = target;
            Index = index;
        }

        protected internal override Expression Eval(Context scope)
        {
            return Expression.Call(
                GetItemMethod,
                Expression.Convert(Target.Eval(scope), typeof(object)),
                Expression.Convert(Index.Eval(scope), typeof(object))
            );

        }

        internal protected override Expression SetVariable(Context scope, Expression right)
        {
            return Expression.Call(
                SetItemMethod,
                Expression.Convert(Target.Eval(scope), typeof(object)),
                Expression.Convert(Index.Eval(scope), typeof(object)),
                Expression.Convert(right, typeof(object))
            );
        }
    }
}
