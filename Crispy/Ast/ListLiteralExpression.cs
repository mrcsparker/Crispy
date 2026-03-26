using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Crispy.Helpers;

namespace Crispy.Ast
{
    sealed class ListLiteralExpression : NodeExpression
    {
        private static readonly MethodInfo CreateListMethod =
            typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CreateList)) ??
            throw new InvalidOperationException("RuntimeHelpers.CreateList was not found.");

        public IReadOnlyList<NodeExpression> Elements { get; }

        public ListLiteralExpression(IReadOnlyList<NodeExpression> elements)
        {
            Elements = elements;
        }

        protected internal override Expression Eval(Context scope)
        {
            var expressions = new Expression[Elements.Count];
            for (var i = 0; i < expressions.Length; i++)
            {
                expressions[i] = Expression.Convert(Elements[i].Eval(scope), typeof(object));
            }

            return Expression.Call(
                CreateListMethod,
                Expression.NewArrayInit(typeof(object), expressions));
        }
    }
}
