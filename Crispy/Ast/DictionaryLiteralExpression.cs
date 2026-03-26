using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Crispy.Helpers;

namespace Crispy.Ast
{
    sealed class DictionaryLiteralExpression : NodeExpression
    {
        private static readonly MethodInfo CreateDictionaryMethod =
            typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CreateDictionary)) ??
            throw new InvalidOperationException("RuntimeHelpers.CreateDictionary was not found.");

        public IReadOnlyList<KeyValuePair<NodeExpression, NodeExpression>> Entries { get; }

        public DictionaryLiteralExpression(IReadOnlyList<KeyValuePair<NodeExpression, NodeExpression>> entries)
        {
            Entries = entries;
        }

        protected internal override Expression Eval(Context scope)
        {
            var expressions = new Expression[Entries.Count * 2];
            for (var i = 0; i < Entries.Count; i++)
            {
                expressions[i * 2] = Expression.Convert(Entries[i].Key.Eval(scope), typeof(object));
                expressions[(i * 2) + 1] = Expression.Convert(Entries[i].Value.Eval(scope), typeof(object));
            }

            return Expression.Call(
                CreateDictionaryMethod,
                Expression.NewArrayInit(typeof(object), expressions));
        }
    }
}
