using System.Dynamic;
using System.Linq.Expressions;
using Crispy.Helpers;

namespace Crispy.Binders
{
    public class CrispyUnaryOperationBinder : UnaryOperationBinder {
        public CrispyUnaryOperationBinder(ExpressionType operation)
            : base(operation) {
        }

        public override DynamicMetaObject FallbackUnaryOperation(
            DynamicMetaObject target, DynamicMetaObject errorSuggestion) {
            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!target.HasValue) {
                return Defer(target);
            }
            return new DynamicMetaObject(
                RuntimeHelpers.EnsureObjectResult(
                    Expression.MakeUnary(
                        this.Operation,
                        Expression.Convert(target.Expression, target.LimitType),
                        target.LimitType)),
                target.Restrictions.Merge(
                    BindingRestrictions.GetTypeRestriction(
                        target.Expression, target.LimitType)));
        }
    }

}

