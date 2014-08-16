using System.Linq.Expressions;

namespace Crispy.Ast
{
    class AssignmentExpression : NodeExpression
    {
        private readonly NodeExpression _left;
        private readonly NodeExpression _right;

        public AssignmentExpression(NodeExpression left, NodeExpression right)
        {
            _left = left;
            _right = right;
        }

        protected internal override Expression Eval(Context scope)
        {
            return _left.SetVariable(scope, _right.Eval(scope));   
        }
    } 
}
