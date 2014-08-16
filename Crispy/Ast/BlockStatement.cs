using System.Collections.Generic;
using System.Linq.Expressions;

namespace Crispy.Ast
{
    class BlockStatement : NodeExpression
    {
        private readonly List<NodeExpression> _statements;

        public BlockStatement(List<NodeExpression> statements)
        {
            _statements = statements;
        }

        protected internal override Expression Eval(Context scope)
        {
            if (_statements.Count == 1)
            {
                var justOne = _statements[0].Eval(scope);

                return Expression.Block(justOne.Type, justOne);
            }

            var statements = new Expression[_statements.Count];
            for (var i = 0; i < statements.Length; i++)
            {
                statements[i] = _statements[i].Eval(scope);
            }

            return Expression.Block(typeof(object), statements);
        }

        public List<NodeExpression> Statements
        {
            get { return _statements; }
        }
    }
}
