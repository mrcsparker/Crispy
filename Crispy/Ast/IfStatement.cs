using System.Collections.Generic;
using System.Linq.Expressions;

namespace Crispy.Ast
{
    class IfStatement : NodeExpression
    {
        private readonly NodeExpression _elseStatement;
        private readonly List<IfStatementTest> _tests;

        public IfStatement(List<IfStatementTest> tests, NodeExpression elseStatement = null)
        {
            _tests = tests;
            _elseStatement = elseStatement;
        }

        protected internal override Expression Eval(Context scope)
        {
            Expression result;
            result = _elseStatement != null ? _elseStatement.Eval(scope) : Expression.Constant(false);

            int index = _tests.Count;
            while (index-- > 0)
            {
                IfStatementTest st = _tests[index];

                result = Expression.Condition(WrapBooleanTest(st.Test.Eval(scope)),
                    Expression.Convert(st.Body.Eval(scope), typeof(object)),
                    Expression.Convert(result, typeof(object)));
            }

            return result;
        }

        private static Expression WrapBooleanTest (Expression expr) {
            var tmp = Expression.Parameter(typeof(object), "testtmp");
            return Expression.Block(
                new[] { tmp },
                new Expression[] 
            {Expression.Assign(tmp, Expression
                .Convert(expr, typeof(object))),
                Expression.Condition(
                    Expression.TypeIs(tmp, typeof(bool)),
                    Expression.Convert(tmp, typeof(bool)),
                    Expression.NotEqual(
                        tmp, 
                        Expression.Constant(null, typeof(object))))});
        }
    }

    class IfStatementTest
    {
        private readonly NodeExpression _body;
        private readonly NodeExpression _test;

        public IfStatementTest(NodeExpression test, NodeExpression body)
        {
            _test = test;
            _body = body;
        }

        public NodeExpression Test
        {
            get { return _test; }
        }

        public NodeExpression Body
        {
            get { return _body; }
        }
    }
}
