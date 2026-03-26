using System;
using System.Dynamic;
using System.Linq.Expressions;
using Crispy.Helpers;

namespace Crispy.Binders
{
    internal sealed class CrispyUnaryOperationBinder : UnaryOperationBinder
    {
        public CrispyUnaryOperationBinder(ExpressionType operation)
            : base(operation)
        {
        }

        public override DynamicMetaObject FallbackUnaryOperation(
            DynamicMetaObject target, DynamicMetaObject? errorSuggestion)
        {
            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!target.HasValue)
            {
                return Defer(target);
            }
            var operandType = target.Value?.GetType() ?? target.LimitType;
            return new DynamicMetaObject(
                RuntimeHelpers.EnsureObjectResult(
                    Expression.MakeUnary(
                        this.Operation,
                        Expression.Convert(target.Expression, operandType),
                        operandType)),
                target.Restrictions.Merge(GetOperandRestriction(target, operandType)));
        }

        private static BindingRestrictions GetOperandRestriction(DynamicMetaObject operand, Type operandType)
        {
            return operand.Value == null
                ? BindingRestrictions.GetInstanceRestriction(operand.Expression, null)
                : BindingRestrictions.GetTypeRestriction(operand.Expression, operandType);
        }
    }

}
