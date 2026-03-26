using System;
using System.Dynamic;
using System.Linq.Expressions;
using Crispy.Helpers;

namespace Crispy.Binders
{
    internal sealed class CrispyBinaryOperationBinder : BinaryOperationBinder
    {
        public CrispyBinaryOperationBinder(ExpressionType operation)
            : base(operation)
        {
        }

        public override DynamicMetaObject FallbackBinaryOperation(
            DynamicMetaObject target, DynamicMetaObject arg,
            DynamicMetaObject? errorSuggestion)
        {
            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!target.HasValue || !arg.HasValue)
            {
                return Defer(target, arg);
            }
            var leftType = target.Value?.GetType() ?? target.LimitType;
            var rightType = arg.Value?.GetType() ?? arg.LimitType;
            var restrictions = target.Restrictions.Merge(arg.Restrictions)
                .Merge(GetOperandRestriction(target, leftType))
                .Merge(GetOperandRestriction(arg, rightType));

            return new DynamicMetaObject(
                RuntimeHelpers.EnsureObjectResult(
                    Expression.MakeBinary(
                        Operation,
                        Expression.Convert(target.Expression, leftType),
                        Expression.Convert(arg.Expression, rightType))),
                restrictions
            );
        }

        private static BindingRestrictions GetOperandRestriction(DynamicMetaObject operand, Type operandType)
        {
            return operand.Value == null
                ? BindingRestrictions.GetInstanceRestriction(operand.Expression, null)
                : BindingRestrictions.GetTypeRestriction(operand.Expression, operandType);
        }

        internal enum BinaryOperationType
        {
            Relational,
            Logical,
            Numeric
        }
    }
}
