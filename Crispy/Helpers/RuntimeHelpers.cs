using System;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

namespace Crispy.Helpers
{
    // RuntimeHelpers is a collection of functions that perform operations at
    // runtime of Crispy code, such as performing an import or eq.
    //
    public static class RuntimeHelpers
    {
        // CrispyImport takes the runtime and module as context for the import.
        // It takes a list of names, what, that either identify a (possibly dotted
        // sequence) of names to fetch from Globals or a file name to load.  Variables
        // is a list of names to fetch from the final object that what indicates
        // and then set each name in module.  Renames is a list of names to add to
        // module instead of names.  If names is empty, then the name set in
        // module is the last name in what.  If renames is not empty, it must have
        // the same cardinality as names.
        //
        public static object CrispyImport(Crispy runtime, ExpandoObject module,
            string[] names, string[] nameAsArr)
        {
            var nameAs = nameAsArr [0];

            // Get object or file scope.
            object value = null;
            if (names.Length == 1) {
                string name = names[0];
                if (DynamicObjectHelpers.HasMember(runtime.Globals, name)) {
                    value = DynamicObjectHelpers.GetMember(runtime.Globals, name);
                } else {
                    string f = (string)(DynamicObjectHelpers
                        .GetMember(module, "__file__"));
                    f = Path.Combine(Path.GetDirectoryName(f), name + ".sympl");
                    if (File.Exists(f)) {
                        value = runtime.ExecuteFile(f);
                    } else {
                        throw new ArgumentException(
                            "Import: can't find name in globals " +
                            "or as file to load -- " + name + " " + f);
                    }
                }
            } else {
                // What has more than one name, must be Globals access.
                value = runtime.Globals;
                // For more correctness and generality, shouldn't assume all
                // globals are dynamic objects, or that a look up like foo.bar.baz
                // cascades through all dynamic objects.
                // Would need to manually create a CallSite here with Crispy's
                // GetMemberBinder, and think about a caching strategy per name.
                foreach (string name in names) {
                    value = DynamicObjectHelpers.GetMember(
                        (IDynamicMetaObjectProvider)value, name);
                }
            }

            if (nameAs != null)
            {
                DynamicObjectHelpers.SetMember(
                    (IDynamicMetaObjectProvider)module, nameAs,
                    value);
            } else
            {
                DynamicObjectHelpers.SetMember(
                    (IDynamicMetaObjectProvider)module, names[names.Length - 1], 
                    value);
            }

            return null;
        } // CrispyImport

        public static object GetItem(object target, object index)
        {
            Type type = target.GetType();
            MethodInfo method = type.GetMethod("get_Item");
            if (method != null)
            {
                return method.Invoke(target, new object[] { index });
            } 

            throw new InvalidOperationException("Cannot get free item from " + type.Name);
        }

        public static object SetItem(object target, object index, object value) {
            Type type = target.GetType();
            MethodInfo method = type.GetMethod("set_Item");
            if (method != null) {
                method.Invoke(target, new object[] { index, value });
            } else {
                throw new InvalidOperationException("Cannot set item on " + type.Name);
            }
            return value;
        }

        ///////////////////////////////////////
        // Utilities used by binders at runtime
        ///////////////////////////////////////

        // ParamsMatchArgs returns whether the args are assignable to the parameters.
        // We specially check for our TypeModel that wraps .NET's RuntimeType, and
        // elsewhere we detect the same situation to convert the TypeModel for calls.
        //
        // Consider checking p.IsByRef and returning false since that's not CLS.
        //
        // Could check for a.HasValue and a.Value is None and
        // ((paramtype is class or interface) or (paramtype is generic and
        // nullable<t>)) to support passing nil anywhere.
        //
        public static bool ParametersMatchArguments(ParameterInfo[] parameters,
                                                    DynamicMetaObject[] args)
        {
            // We only call this after filtering members by this constraint.
            Debug.Assert(args.Length == parameters.Length,
                         "Internal: args are not same len as params?!");
            for (var i = 0; i < args.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                // We consider arg of TypeModel and param of Type to be compatible.
                if (paramType == typeof(Type) &&
                    (args[i].LimitType == typeof(TypeModel)))
                {
                    continue;
                }
                if (!paramType
                    // Could check for HasValue and Value==null AND
                    // (paramtype is class or interface) or (is generic
                    // and nullable<T>) ... to bind nullables and null.
                        .IsAssignableFrom(args[i].LimitType))
                {
                    return false;
                }
            }
            return true;
        }

        // Returns a DynamicMetaObject with an expression that fishes the .NET
        // RuntimeType object from the TypeModel MO.
        //
        public static DynamicMetaObject GetRuntimeTypeMoFromModel(
                                              DynamicMetaObject typeModelMO)
        {
            Debug.Assert((typeModelMO.LimitType == typeof(TypeModel)),
                         "Internal: MO is not a TypeModel?!");
            // Get tm.ReflType
            var pi = typeof(TypeModel).GetProperty("ReflType");
            Debug.Assert(pi != null);
            return new DynamicMetaObject(
                Expression.Property(
                    Expression.Convert(typeModelMO.Expression, typeof(TypeModel)),
                    pi),
                typeModelMO.Restrictions.Merge(
                    BindingRestrictions.GetTypeRestriction(
                        typeModelMO.Expression, typeof(TypeModel)))//,
                // Must supply a value to prevent binder FallbackXXX methods
                // from infinitely looping if they do not check this MO for
                // HasValue == false and call Defer.  After Crispy added Defer
                // checks, we could verify, say, FallbackInvokeMember by no
                // longer passing a value here.
                //((TypeModel)typeModelMO.Value).ReflType
            );
        }

        // Returns list of Convert exprs converting args to param types.  If an arg
        // is a TypeModel, then we treat it special to perform the binding.  We need
        // to map from our runtime model to .NET's RuntimeType object to match.
        //
        // To call this function, args and pinfos must be the same length, and param
        // types must be assignable from args.
        //
        // NOTE, if using this function, then need to use GetTargetArgsRestrictions
        // and make sure you're performing the same conversions as restrictions.
        //
        public static Expression[] ConvertArguments(
                                 DynamicMetaObject[] args, ParameterInfo[] ps)
        {
            Debug.Assert(args.Length == ps.Length,
                         "Internal: args are not same len as params?!");
            var callArgs = new Expression[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                Expression argExpr = args[i].Expression;
                if (args[i].LimitType == typeof(TypeModel) &&
                    ps[i].ParameterType == typeof(Type))
                {
                    // Get arg.ReflType
                    argExpr = GetRuntimeTypeMoFromModel(args[i]).Expression;
                }
                argExpr = Expression.Convert(argExpr, ps[i].ParameterType);
                callArgs[i] = argExpr;
            }
            return callArgs;
        }

        // GetTargetArgsRestrictions generates the restrictions needed for the
        // MO resulting from binding an operation.  This combines all existing
        // restrictions and adds some for arg conversions.  targetInst indicates
        // whether to restrict the target to an instance (for operations on type
        // objects) or to a type (for operations on an instance of that type).
        //
        // NOTE, this function should only be used when the caller is converting
        // arguments to the same types as these restrictions.
        //
        public static BindingRestrictions GetTargetArgsRestrictions(
                DynamicMetaObject target, DynamicMetaObject[] args,
                bool instanceRestrictionOnTarget)
        {
            // Important to add existing restriction first because the
            // DynamicMetaObjects (and possibly values) we're looking at depend
            // on the pre-existing restrictions holding true.
            var restrictions = target.Restrictions.Merge(BindingRestrictions
                                                            .Combine(args));
            if (instanceRestrictionOnTarget)
            {
                restrictions = restrictions.Merge(
                    BindingRestrictions.GetInstanceRestriction(
                        target.Expression,
                        target.Value
                    ));
            }
            else
            {
                restrictions = restrictions.Merge(
                    BindingRestrictions.GetTypeRestriction(
                        target.Expression,
                        target.LimitType
                    ));
            }
            for (int i = 0; i < args.Length; i++)
            {
                BindingRestrictions r;
                if (args[i].HasValue && args[i].Value == null)
                {
                    r = BindingRestrictions.GetInstanceRestriction(
                            args[i].Expression, null);
                }
                else
                {
                    r = BindingRestrictions.GetTypeRestriction(
                            args[i].Expression, args[i].LimitType);
                }
                restrictions = restrictions.Merge(r);
            }
            return restrictions;
        }

        // CreateThrow is a convenience function for when binders cannot bind.
        // They need to return a DynamicMetaObject with appropriate restrictions
        // that throws.  Binders never just throw due to the protocol since
        // a binder or MO down the line may provide an implementation.
        //
        // It returns a DynamicMetaObject whose expr throws the exception, and 
        // ensures the expr's type is object to satisfy the CallSite return type
        // constraint.
        //
        // A couple of calls to CreateThrow already have the args and target
        // restrictions merged in, but BindingRestrictions.Merge doesn't add 
        // duplicates.
        //
        public static DynamicMetaObject CreateThrow
                (DynamicMetaObject target, DynamicMetaObject[] args,
                 BindingRestrictions moreTests,
                 Type exception, params object[] exceptionArgs)
        {
            Expression[] argExprs = null;
            Type[] argTypes = Type.EmptyTypes;
            int i;
            if (exceptionArgs != null)
            {
                i = exceptionArgs.Length;
                argExprs = new Expression[i];
                argTypes = new Type[i];
                i = 0;
                foreach (object o in exceptionArgs)
                {
                    Expression e = Expression.Constant(o);
                    argExprs[i] = e;
                    argTypes[i] = e.Type;
                    i += 1;
                }
            }
            ConstructorInfo constructor = exception.GetConstructor(argTypes);
            if (constructor == null)
            {
                throw new ArgumentException("Type doesn't have constructor with a given signature");
            }
            return new DynamicMetaObject(
                Expression.Throw(
                    Expression.New(constructor, argExprs),
                // Force expression to be type object so that DLR CallSite
                // code things only type object flows out of the CallSite.
                    typeof(object)),
                target.Restrictions.Merge(BindingRestrictions.Combine(args))
                                   .Merge(moreTests));
        }

        // EnsureObjectResult wraps expr if necessary so that any binder or
        // DynamicMetaObject result expression returns object.  This is required
        // by CallSites.
        //
        public static Expression EnsureObjectResult(Expression expr)
        {
            if (!expr.Type.IsValueType)
                return expr;
            if (expr.Type == typeof(void))
                return Expression.Block(expr, Expression.Default(typeof(object)));
            return Expression.Convert(expr, typeof(object));
        }

    }
}

