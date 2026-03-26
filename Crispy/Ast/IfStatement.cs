using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Crispy.Helpers;

namespace Crispy.Ast
{
    sealed class IfStatement : NodeExpression
    {
        private static readonly MethodInfo IsTruthyMethod =
            typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.IsTruthy)) ??
            throw new InvalidOperationException("RuntimeHelpers.IsTruthy was not found.");

        private NodeExpression? ElseStatement { get; }
        private List<IfStatementTest> Tests { get; }

        public IfStatement(List<IfStatementTest> tests, NodeExpression? elseStatement = null)
        {
            Tests = tests;
            ElseStatement = elseStatement;
        }

        protected internal override Expression Eval(Context scope)
        {
            Expression result;
            result = ElseStatement != null ? ElseStatement.Eval(scope) : Expression.Constant(false);

            int index = Tests.Count;
            while (index-- > 0)
            {
                IfStatementTest st = Tests[index];

                result = Expression.Condition(WrapBooleanTest(st.Test.Eval(scope)),
                    Expression.Convert(st.Body.Eval(scope), typeof(object)),
                    Expression.Convert(result, typeof(object)));
            }

            return result;
        }

        private static BlockExpression WrapBooleanTest(Expression expr)
        {
            return Expression.Block(
                Expression.Call(
                    IsTruthyMethod,
                    Expression.Convert(expr, typeof(object))));
        }
    }

    sealed class IfStatementTest
    {
        public NodeExpression Test { get; }
        public NodeExpression Body { get; }

        public IfStatementTest(NodeExpression test, NodeExpression body)
        {
            Test = test;
            Body = body;
        }
    }
}
