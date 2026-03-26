using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Crispy.Binders;

namespace Crispy.Helpers
{
    // RuntimeHelpers is a collection of functions that perform operations at
    // runtime of Crispy code, such as performing an import or eq.
    //
    internal static class RuntimeHelpers
    {
        private static readonly string[] ScriptExtensions = [".crispy", ".sympl"];

        public static object? ResolveImport(CrispyRuntime runtime, ExpandoObject module, string[] names)
        {
            ArgumentNullException.ThrowIfNull(runtime);
            ArgumentNullException.ThrowIfNull(module);
            ArgumentNullException.ThrowIfNull(names);

            // Get object or file scope.
            object? value = null;
            if (names.Length == 1)
            {
                string name = names[0];
                if (DynamicObjectHelpers.HasMember(runtime.Globals, name))
                {
                    value = DynamicObjectHelpers.GetMember(runtime.Globals, name);
                }
                else
                {
                    string filePath = (string)(DynamicObjectHelpers
                        .GetMember(module, "__file__"));
                    string directory = Path.GetDirectoryName(filePath) ??
                        throw new ArgumentException("Import: module file path does not have a directory.");
                    var importPath = FindImportPath(directory, name);
                    if (importPath != null)
                    {
                        value = runtime.ExecuteFile(importPath);
                    }
                    else
                    {
                        throw new ArgumentException(
                            "Import: can't find name in globals " +
                            "or as file to load -- " + name + " " + filePath);
                    }
                }
            }
            else
            {
                // What has more than one name, must be Globals access.
                value = runtime.Globals;
                // For more correctness and generality, shouldn't assume all
                // globals are dynamic objects, or that a look up like foo.bar.baz
                // cascades through all dynamic objects.
                // Would need to manually create a CallSite here with Crispy's
                // GetMemberBinder, and think about a caching strategy per name.
                foreach (string name in names)
                {
                    value = DynamicObjectHelpers.GetMember(
                        (IDynamicMetaObjectProvider)value, name);
                }
            }

            return value;
        }

        // CrispyImport takes the runtime and module as context for the import.
        // It takes a list of names, what, that either identify a (possibly dotted
        // sequence) of names to fetch from Globals or a file name to load.  Variables
        // is a list of names to fetch from the final object that what indicates
        // and then set each name in module.  Renames is a list of names to add to
        // module instead of names.  If names is empty, then the name set in
        // module is the last name in what.  If renames is not empty, it must have
        // the same cardinality as names.
        //
        public static object? CrispyImport(CrispyRuntime runtime, ExpandoObject module,
            string[] names, string[] nameAsArr)
        {
            ArgumentNullException.ThrowIfNull(nameAsArr);
            var nameAs = nameAsArr[0];
            var value = ResolveImport(runtime, module, names);

            if (nameAs != null)
            {
                DynamicObjectHelpers.SetMember(
                    (IDynamicMetaObjectProvider)module, nameAs,
                    value);
            }
            else
            {
                DynamicObjectHelpers.SetMember(
                    (IDynamicMetaObjectProvider)module, names[names.Length - 1],
                    value);
            }

            return null;
        } // CrispyImport

        public static object? GetItem(object target, object index)
        {
            ArgumentNullException.ThrowIfNull(target);
            Type type = target.GetType();
            MethodInfo? method = type.GetMethod("get_Item");
            return method != null
                ? method.Invoke(target, new object[] { index })
                : throw new InvalidOperationException("Cannot get free item from " + type.Name);
        }

        private static string? FindImportPath(string directory, string name)
        {
            foreach (var extension in ScriptExtensions)
            {
                var candidate = Path.Combine(directory, name + extension);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        public static object? SetItem(object target, object index, object? value)
        {
            ArgumentNullException.ThrowIfNull(target);
            Type type = target.GetType();
            MethodInfo? method = type.GetMethod("set_Item");
            if (method != null)
            {
                method.Invoke(target, new object?[] { index, value });
            }
            else
            {
                throw new InvalidOperationException("Cannot set item on " + type.Name);
            }
            return value;
        }

        public static bool IsTruthy(object? value)
        {
            return value is bool booleanValue
                ? booleanValue
                : value != null;
        }

        public static ArrayList CreateList(object?[] values)
        {
            ArgumentNullException.ThrowIfNull(values);

            var list = new ArrayList(values.Length);
            foreach (var value in values)
            {
                list.Add(value);
            }

            return list;
        }

        public static Hashtable CreateDictionary(object?[] entries)
        {
            ArgumentNullException.ThrowIfNull(entries);
            if (entries.Length % 2 != 0)
            {
                throw new ArgumentException("Dictionary entries must contain key/value pairs.", nameof(entries));
            }

            var dictionary = new Hashtable(entries.Length / 2);
            for (var i = 0; i < entries.Length; i += 2)
            {
                var key = entries[i] ?? throw new ArgumentException(
                    "Dictionary literal keys cannot be null.",
                    nameof(entries));
                dictionary[key] = entries[i + 1];
            }

            return dictionary;
        }

        public static IEnumerator GetEnumerator(object? value)
        {
            return value is IEnumerable enumerable
                ? enumerable.GetEnumerator()
                : throw new InvalidOperationException("Cannot iterate over " + (value?.GetType().Name ?? "null"));
        }

        public static bool MoveNext(IEnumerator enumerator)
        {
            ArgumentNullException.ThrowIfNull(enumerator);
            return enumerator.MoveNext();
        }

        public static object? GetCurrent(IEnumerator enumerator)
        {
            ArgumentNullException.ThrowIfNull(enumerator);
            return enumerator.Current;
        }

        public static void DisposeEnumerator(IEnumerator enumerator)
        {
            ArgumentNullException.ThrowIfNull(enumerator);

            if (enumerator is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public static Exception CoerceToException(object? value)
        {
            return value as Exception ?? new CrispyThrownValueException(value);
        }

        public static object? GetCaughtValue(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return exception is CrispyThrownValueException crispyException
                ? crispyException.Value
                : exception;
        }

        public static object? GetExpandoMember(ExpandoObject target, string memberName)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(memberName);

            var dictionary = (IDictionary<string, object?>)target;
            if (dictionary.TryGetValue(memberName, out var value))
            {
                return value;
            }

            foreach (var entry in dictionary)
            {
                if (string.Equals(entry.Key, memberName, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.Value;
                }
            }

            throw new MissingMemberException(
                "cannot bind member, " + memberName +
                ", on object " + target.GetType());
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
            ArgumentNullException.ThrowIfNull(parameters);
            ArgumentNullException.ThrowIfNull(args);
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
                // Could check for HasValue and Value==null AND
                // (paramtype is class or interface) or (is generic
                // and nullable<T>) ... to bind nullables and null.
                if (!paramType.IsAssignableFrom(args[i].LimitType))
                {
                    return false;
                }
            }
            return true;
        }

        public static (MethodInfo? Method, string? ErrorMessage) ResolveMethodOverload(
            IEnumerable<MethodInfo> methods,
            DynamicMetaObject[] args,
            string subject)
        {
            ArgumentNullException.ThrowIfNull(methods);
            ArgumentNullException.ThrowIfNull(args);
            ArgumentNullException.ThrowIfNull(subject);

            return ResolveOverload(
                methods,
                args.Length,
                static method => method.GetParameters(),
                static method => method.ContainsGenericParameters,
                i => args[i].LimitType,
                i => args[i].HasValue && args[i].Value == null,
                i => args[i].LimitType == typeof(TypeModel),
                subject);
        }

        public static (ConstructorInfo? Constructor, string? ErrorMessage) ResolveConstructorOverload(
            IEnumerable<ConstructorInfo> constructors,
            DynamicMetaObject[] args,
            string subject)
        {
            ArgumentNullException.ThrowIfNull(constructors);
            ArgumentNullException.ThrowIfNull(args);
            ArgumentNullException.ThrowIfNull(subject);

            return ResolveOverload(
                constructors,
                args.Length,
                static constructor => constructor.GetParameters(),
                static _ => false,
                i => args[i].LimitType,
                i => args[i].HasValue && args[i].Value == null,
                static _ => false,
                subject);
        }

        public static (MethodInfo? Method, string? ErrorMessage) ResolveMethodOverload(
            IEnumerable<MethodInfo> methods,
            Type[] argumentTypes,
            string subject)
        {
            ArgumentNullException.ThrowIfNull(methods);
            ArgumentNullException.ThrowIfNull(argumentTypes);
            ArgumentNullException.ThrowIfNull(subject);

            return ResolveOverload(
                methods,
                argumentTypes.Length,
                static method => method.GetParameters(),
                static method => method.ContainsGenericParameters,
                i => argumentTypes[i],
                static _ => false,
                static _ => false,
                subject);
        }

        private static (TMember? Member, string? ErrorMessage) ResolveOverload<TMember>(
            IEnumerable<TMember> members,
            int argumentCount,
            Func<TMember, ParameterInfo[]> getParameters,
            Func<TMember, bool> isGenericDefinition,
            Func<int, Type> getArgumentType,
            Func<int, bool> isNullArgument,
            Func<int, bool> isTypeModelArgument,
            string subject)
            where TMember : MethodBase
        {
            var bestMatches = new List<TMember>();
            var bestScore = int.MaxValue;
            var sawOptionalOnlyMatch = false;
            var sawByRefOrOut = false;
            var sawGenericMethod = false;

            foreach (var member in members)
            {
                if (isGenericDefinition(member))
                {
                    sawGenericMethod = true;
                    continue;
                }

                var parameters = getParameters(member);
                if (parameters.Any(static parameter => parameter.IsOut || parameter.ParameterType.IsByRef))
                {
                    sawByRefOrOut = true;
                    continue;
                }

                if (argumentCount < parameters.Length)
                {
                    if (parameters.Skip(argumentCount).All(static parameter => parameter.IsOptional))
                    {
                        sawOptionalOnlyMatch = true;
                    }

                    continue;
                }

                if (argumentCount > parameters.Length)
                {
                    continue;
                }

                var totalScore = 0;
                var applicable = true;
                for (var i = 0; i < argumentCount; i++)
                {
                    if (!TryScoreArgument(
                        parameters[i].ParameterType,
                        getArgumentType(i),
                        isNullArgument(i),
                        isTypeModelArgument(i),
                        out var parameterScore))
                    {
                        applicable = false;
                        break;
                    }

                    totalScore += parameterScore;
                }

                if (!applicable)
                {
                    continue;
                }

                if (totalScore < bestScore)
                {
                    bestScore = totalScore;
                    bestMatches.Clear();
                    bestMatches.Add(member);
                }
                else if (totalScore == bestScore)
                {
                    bestMatches.Add(member);
                }
            }

            if (bestMatches.Count == 1)
            {
                return (bestMatches[0], null);
            }

            if (bestMatches.Count > 1)
            {
                return (null, "Ambiguous overload for " + subject + ": " +
                    string.Join("; ", bestMatches.Select(FormatSignature)));
            }

            if (sawOptionalOnlyMatch)
            {
                return (null, "Optional parameters are not supported for " + subject + "; pass all arguments explicitly.");
            }

            if (sawByRefOrOut)
            {
                return (null, "ref/out parameters are not supported for " + subject + ".");
            }

            if (sawGenericMethod)
            {
                return (null, "Generic methods are not supported for " + subject + ".");
            }

            return (null, null);
        }

        private static bool TryScoreArgument(
            Type parameterType,
            Type argumentType,
            bool isNullArgument,
            bool isTypeModelArgument,
            out int score)
        {
            parameterType = parameterType.IsByRef
                ? parameterType.GetElementType() ?? parameterType
                : parameterType;

            if (isNullArgument)
            {
                if (AllowsNull(parameterType))
                {
                    score = 50;
                    return true;
                }

                score = 0;
                return false;
            }

            if (isTypeModelArgument && parameterType == typeof(Type))
            {
                score = 0;
                return true;
            }

            if (parameterType == argumentType)
            {
                score = 0;
                return true;
            }

            if (parameterType.IsAssignableFrom(argumentType))
            {
                score = GetAssignableConversionScore(parameterType, argumentType);
                return true;
            }

            return TryGetNumericWideningScore(argumentType, parameterType, out score);
        }

        private static bool AllowsNull(Type parameterType)
        {
            return !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) != null;
        }

        private static int GetAssignableConversionScore(Type parameterType, Type argumentType)
        {
            if (parameterType == argumentType)
            {
                return 0;
            }

            if (parameterType.IsInterface)
            {
                return 30;
            }

            if (argumentType.IsValueType)
            {
                return 40;
            }

            var distance = 0;
            var current = argumentType;
            while (current != null && current != parameterType)
            {
                current = current.BaseType!;
                distance += 1;
            }

            return 10 + distance;
        }

        private static bool TryGetNumericWideningScore(Type argumentType, Type parameterType, out int score)
        {
            argumentType = Nullable.GetUnderlyingType(argumentType) ?? argumentType;
            parameterType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;

            score = 0;
            if (argumentType == parameterType)
            {
                return false;
            }

            if (!TryGetImplicitNumericTargets(argumentType, out var targets))
            {
                return false;
            }

            var index = Array.IndexOf(targets, parameterType);
            if (index < 0)
            {
                return false;
            }

            score = 100 + index;
            return true;
        }

        private static bool TryGetImplicitNumericTargets(Type type, out Type[] targets)
        {
            targets = Type.GetTypeCode(type) switch
            {
                TypeCode.SByte => [typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
                TypeCode.Byte => [typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
                TypeCode.Int16 => [typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
                TypeCode.UInt16 => [typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
                TypeCode.Int32 => [typeof(long), typeof(float), typeof(double), typeof(decimal)],
                TypeCode.UInt32 => [typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
                TypeCode.Int64 => [typeof(float), typeof(double), typeof(decimal)],
                TypeCode.UInt64 => [typeof(float), typeof(double), typeof(decimal)],
                TypeCode.Char => [typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
                TypeCode.Single => [typeof(double)],
                _ => Type.EmptyTypes
            };

            return targets.Length > 0;
        }

        private static string FormatSignature(MethodBase member)
        {
            var declaringType = member.DeclaringType?.FullName ?? member.DeclaringType?.Name ?? "<unknown>";
            var parameters = string.Join(", ",
                member.GetParameters().Select(static parameter =>
                {
                    var parameterType = parameter.ParameterType.IsByRef
                        ? parameter.ParameterType.GetElementType() ?? parameter.ParameterType
                        : parameter.ParameterType;
                    return parameterType.FullName ?? parameterType.Name;
                }));

            return member is ConstructorInfo
                ? declaringType + "(" + parameters + ")"
                : declaringType + "." + member.Name + "(" + parameters + ")";
        }

        // Returns a DynamicMetaObject with an expression that fishes the .NET
        // RuntimeType object from the TypeModel MO.
        //
        public static DynamicMetaObject GetRuntimeTypeMoFromModel(
                                              DynamicMetaObject typeModelMO)
        {
            ArgumentNullException.ThrowIfNull(typeModelMO);
            Debug.Assert((typeModelMO.LimitType == typeof(TypeModel)),
                         "Internal: MO is not a TypeModel?!");
            // Get tm.ReflType
            var pi = typeof(TypeModel).GetProperty("ReflType") ??
                throw new InvalidOperationException("TypeModel.ReflType property was not found.");
            // We no longer pass an explicit value here. If a fallback binder
            // does not check HasValue and calls Defer, that can loop. After
            // Crispy added Defer checks, this should remain safe.
            return new DynamicMetaObject(
                Expression.Property(
                    Expression.Convert(typeModelMO.Expression, typeof(TypeModel)),
                    pi),
                typeModelMO.Restrictions.Merge(
                    BindingRestrictions.GetTypeRestriction(
                        typeModelMO.Expression,
                        typeof(TypeModel))));
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
            ArgumentNullException.ThrowIfNull(args);
            ArgumentNullException.ThrowIfNull(ps);
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
                else if (argExpr.Type != args[i].LimitType)
                {
                    argExpr = Expression.Convert(argExpr, args[i].LimitType);
                }

                if (argExpr.Type != ps[i].ParameterType)
                {
                    argExpr = Expression.Convert(argExpr, ps[i].ParameterType);
                }

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
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(args);
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
                (DynamicMetaObject target, DynamicMetaObject[]? args,
                 BindingRestrictions moreTests,
                 Type exception, params object[] exceptionArgs)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(exception);
            Expression[] argExprs = Array.Empty<Expression>();
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
            ConstructorInfo? constructor = exception.GetConstructor(argTypes);
            return constructor == null
                ? throw new ArgumentException("Type doesn't have constructor with a given signature")
                : new DynamicMetaObject(
                    Expression.Throw(
                        Expression.New(constructor, argExprs),
                        // Force expression to be type object so that DLR CallSite
                        // code things only type object flows out of the CallSite.
                        typeof(object)),
                    target.Restrictions
                          .Merge(BindingRestrictions.Combine(args ?? Array.Empty<DynamicMetaObject>()))
                          .Merge(moreTests));
        }

        public static DynamicExpression MakeInvokeExpression(Expression target, DynamicMetaObject[] args)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(args);

            var argExpressions = new Expression[args.Length + 1];
            argExpressions[0] = EnsureObjectResult(target);
            for (var i = 0; i < args.Length; i++)
            {
                argExpressions[i + 1] = EnsureObjectResult(args[i].Expression);
            }

            return Expression.Dynamic(
                new CrispyInvokeBinder(new CallInfo(args.Length)),
                typeof(object),
                argExpressions);
        }

        // EnsureObjectResult wraps expr if necessary so that any binder or
        // DynamicMetaObject result expression returns object.  This is required
        // by CallSites.
        //
        public static Expression EnsureObjectResult(Expression expr)
        {
            ArgumentNullException.ThrowIfNull(expr);
            return !expr.Type.IsValueType
                ? expr
                : expr.Type == typeof(void)
                    ? Expression.Block(expr, Expression.Default(typeof(object)))
                    : Expression.Convert(expr, typeof(object));
        }

    }
}
