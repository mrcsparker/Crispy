using System.Linq.Expressions;

namespace Crispy.Ast
{
    sealed class MemberExpression : NodeExpression
    {
        public NodeExpression Expr { get; }
        public override string Name { get; }
        public MemberType Type { get; }

        public MemberExpression(NodeExpression expr, string name, MemberType type)
        {
            Expr = expr;
            Name = name;
            Type = type;
        }

        protected internal override Expression Eval(Context scope)
        {
            return Type == MemberType.MethodCall
                ? Expr.Eval(scope)
                : Expression.Dynamic(
                    scope.Runtime.GetGetMemberBinder(Name),
                    typeof(object),
                    Expr.Eval(scope)
                );
        }

        internal protected override Expression SetVariable(Context scope, Expression right)
        {
            return Expression.Dynamic(
                scope.Runtime.GetSetMemberBinder(Name),
                typeof(object),
                Expr.Eval(scope), right);
        }

        public override bool IsMember
        {
            get { return true; }
        }

    }

    enum MemberType
    {
        Member,
        MethodCall
    };
}
