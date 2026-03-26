using System.Linq.Expressions;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace Crispy.Ast
{
    sealed class NewExpression : NodeExpression
    {
        public NodeExpression Target { get; }
        public NodeExpression[] Arguments { get; }

        public NewExpression(NodeExpression target, NodeExpression[] arguments)
        {
            Target = target;
            Arguments = arguments;
        }

        protected internal override Expression Eval(Context scope)
        {
            var args = new List<Expression> { Target.Eval(scope) };
            args.AddRange(Arguments.Select(a => a.Eval(scope)));

            return Expression.Dynamic(
                scope.Runtime.GetCreateInstanceBinder(
                    new CallInfo(Arguments.Length)),
                typeof(object),
                args
            );
        }
    }
}
