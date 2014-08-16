using System;
using System.Dynamic;
using Crispy.Helpers;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;

namespace Crispy.Binders
{
    public class CrispyCreateInstanceBinder : CreateInstanceBinder {
        public CrispyCreateInstanceBinder(CallInfo callinfo)
            : base(callinfo) {
        }

        public override DynamicMetaObject FallbackCreateInstance(
            DynamicMetaObject target,
            DynamicMetaObject[] args,
            DynamicMetaObject errorSuggestion) {
            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.

            if (!target.HasValue || args.Any(a => !a.HasValue)) {
                var deferArgs = new DynamicMetaObject[args.Length + 1];
                for (int i = 0; i < args.Length; i++) {
                    deferArgs[i + 1] = args[i];
                }
                deferArgs[0] = target;
                return Defer(deferArgs);
            }
            // Make sure target actually contains a Type.
            if (!typeof(Type).IsAssignableFrom(target.LimitType)) {
                return errorSuggestion ??
                    RuntimeHelpers.CreateThrow(
                        target, args, BindingRestrictions.Empty,
                        typeof(InvalidOperationException),
                        "Type object must be used when creating instance -- " +
                        args.ToString());
            }
            var type = target.Value as Type;
            Debug.Assert(type != null);
            var constructors = type.GetConstructors();
            // Get constructors with right arg counts.
            var ctors = constructors.
                Where(c => c.GetParameters().Length == args.Length);
            var res = new List<ConstructorInfo>();
            foreach (var c in ctors) {
                if (RuntimeHelpers.ParametersMatchArguments(c.GetParameters(),
                    args)) {
                    res.Add(c);
                }
            }
            // We generate an instance restriction on the target since it is a
            // Type and the constructor is associate with the actual Type instance.
            var restrictions =
                RuntimeHelpers.GetTargetArgsRestrictions(
                    target, args, true);
            if (res.Count == 0) {
                return errorSuggestion ??
                    RuntimeHelpers.CreateThrow(
                        target, args, restrictions,
                        typeof(MissingMemberException),
                        "Can't bind create instance -- " + args.ToString());
            }
            var ctorArgs =
                RuntimeHelpers.ConvertArguments(
                    args, res[0].GetParameters());
            return new DynamicMetaObject(
                // Creating an object, so don't need EnsureObjectResult.
                Expression.New(res[0], ctorArgs),
                restrictions);
        }
    }
}

