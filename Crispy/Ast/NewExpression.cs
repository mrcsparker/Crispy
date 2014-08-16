using System.Linq.Expressions;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace Crispy.Ast
{
    class NewExpression : NodeExpression
    {
        private readonly NodeExpression _target;
        private readonly NodeExpression[] _arguments;

        public NewExpression(NodeExpression target, NodeExpression[] arguments)
        {
            _target = target;
            _arguments = arguments;
        }

        protected internal override Expression Eval(Context scope)
        {
            var args = new List<Expression> {_target.Eval(scope)};
            args.AddRange(_arguments.Select(a => a.Eval(scope)));

            return Expression.Dynamic(
                scope.GetRuntime().GetCreateInstanceBinder(
                    new CallInfo(_arguments.Length)),
                typeof(object),
                args
            );
        }

        public NodeExpression Target 
        {
            get { return _target; }
        }

        public NodeExpression[] Arguments
        {
            get { return _arguments; }
        }
    }
}
