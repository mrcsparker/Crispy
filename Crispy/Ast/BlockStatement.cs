using System.Collections.Generic;
using System.Linq.Expressions;

namespace Crispy.Ast
{
    sealed class BlockStatement : NodeExpression
    {
        public List<NodeExpression> Statements { get; }

        public BlockStatement(List<NodeExpression> statements)
        {
            Statements = statements;
        }

        protected internal override Expression Eval(Context scope)
        {
            if (Statements.Count == 0)
            {
                return Expression.Constant(null, typeof(object));
            }

            var blockScope = new Context(scope, "block");
            var blockVariables = new List<ParameterExpression>();

            if (Statements.Count == 1)
            {
                var justOne = Statements[0].Eval(blockScope);
                blockVariables.AddRange(blockScope.Variables.Values);

                return blockVariables.Count > 0
                    ? Expression.Block(justOne.Type, blockVariables, justOne)
                    : Expression.Block(justOne.Type, justOne);
            }

            var statements = new Expression[Statements.Count];
            for (var i = 0; i < statements.Length; i++)
            {
                statements[i] = Statements[i].Eval(blockScope);
            }
            blockVariables.AddRange(blockScope.Variables.Values);

            return blockVariables.Count > 0
                ? Expression.Block(typeof(object), blockVariables, statements)
                : Expression.Block(typeof(object), statements);
        }
    }
}
