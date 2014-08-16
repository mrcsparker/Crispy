using System.Dynamic;
using System.Linq.Expressions;
using Crispy.Helpers;

namespace Crispy.Binders
{
    class DoHelpersGetMemberBinder : GetMemberBinder {

        internal DoHelpersGetMemberBinder(string name) : base(name, true) { }

        public override DynamicMetaObject FallbackGetMember(
            DynamicMetaObject target, DynamicMetaObject errorSuggestion) {
            return errorSuggestion ??
                new DynamicMetaObject(
                    Expression.Constant(DynamicObjectHelpers.Sentinel),
                    target.Restrictions.Merge(
                        BindingRestrictions.GetTypeRestriction(
                            target.Expression, target.LimitType)));
        }
    }
}

