using System.Linq.Expressions;
using Crispy.Helpers;
using System.Collections.Generic;
using System;

namespace Crispy.Ast
{
    class MemberExpression : NodeExpression
    {
        private readonly NodeExpression _expr;
        private readonly string _name;
        private readonly MemberType _type;

        public MemberExpression(NodeExpression expr, string name, MemberType type)
        {
            _expr = expr;
            _name = name;
            _type = type;
        }

        protected internal override Expression Eval(Context scope)
        {

            if (_type == MemberType.MethodCall)
            {
                return _expr.Eval(scope);
            }

            return Expression.Dynamic(
                scope.GetRuntime().GetGetMemberBinder(_name),
                typeof(object),
                _expr.Eval(scope)
            );

        }

        internal protected override Expression SetVariable(Context scope, Expression right)
        {
            return Expression.Dynamic(
                scope.GetRuntime().GetSetMemberBinder(_name),
                typeof(object),
                _expr.Eval(scope), right);
        }

        public override bool IsMember
        {
            get { return true; }
        }

        public NodeExpression Expr
        {
            get { return _expr; }
        }

        public override string Name {
            get { return _name; }
        }

        public MemberType Type
        {
            get { return _type; }
        }
    }

    enum MemberType {
        Member,
        MethodCall
    };
}
