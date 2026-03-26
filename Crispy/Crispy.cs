using System;
using System.IO;
using System.Linq.Expressions;
using Crispy.Parsing;
using System.Dynamic;
using System.Reflection;
using System.Collections.Generic;
using Crispy.Helpers;
using Crispy.Binders;

namespace Crispy
{
    public class CrispyRuntime
    {
        private Assembly[] Assemblies { get; }
        private readonly List<object> _instanceObjects = [];

        public ExpandoObject Globals { get; } = new ExpandoObject();

        public CrispyRuntime(Assembly[] assms)
        {
            ArgumentNullException.ThrowIfNull(assms);
            Assemblies = assms;
            AddAssemblyNamesAndTypes();
        }

        public CrispyRuntime(Assembly[] assms, object[] instanceObjects)
        {
            ArgumentNullException.ThrowIfNull(assms);
            ArgumentNullException.ThrowIfNull(instanceObjects);
            Assemblies = assms;
            AddAssemblyNamesAndTypes();
            _instanceObjects = [.. instanceObjects];
            AddInstanceObjectNamesAndTypes();
        }

        // _addNamespacesAndTypes builds a tree of ExpandoObjects representing
        // .NET namespaces, with TypeModel objects at the leaves.  Though Crispy is
        // case-insensitive, we store the names as they appear in .NET reflection
        // in case our globals object or a namespace object gets passed as an IDO
        // to another language or library, where they may be looking for names
        // case-sensitively using EO's default lookup.
        //
        private void AddAssemblyNamesAndTypes()
        {
            foreach (var assm in Assemblies)
            {
                foreach (var typ in assm.GetExportedTypes())
                {
                    var fullName = typ.FullName;
                    if (fullName == null)
                    {
                        continue;
                    }

                    string[] names = fullName.Split('.');
                    var table = Globals;
                    for (int i = 0; i < names.Length - 1; i++)
                    {
                        string name = names[i];
                        if (DynamicObjectHelpers.HasMember(table, name))
                        {
                            // Must be Expando since only we have put objs in
                            // the tables so far.
                            table = (ExpandoObject)(DynamicObjectHelpers.GetMember(table, name));
                        }
                        else
                        {
                            var tmp = new ExpandoObject();
                            DynamicObjectHelpers.SetMember(table, name, tmp);
                            table = tmp;
                        }
                    }
                    DynamicObjectHelpers.SetMember(table, names[names.Length - 1], new TypeModel(typ));
                }
            }
        }

        private void AddInstanceObjectNamesAndTypes()
        {
            foreach (var instanceObject in _instanceObjects)
            {
                foreach (var methodName in instanceObject.GetType().GetMethods())
                {
                    var table = Globals;
                    if (DynamicObjectHelpers.HasMember(table, instanceObject.GetType().Name))
                    {
                        table = (ExpandoObject)(DynamicObjectHelpers.GetMember(table, instanceObject.GetType().Name));
                    }
                    else
                    {
                        var tmp = new ExpandoObject();
                        DynamicObjectHelpers.SetMember(table, instanceObject.GetType().Name, tmp);
                        table = tmp;
                    }

                    DynamicObjectHelpers.SetMember(table, methodName.Name, instanceObject);
                    DynamicObjectHelpers.SetMember(Globals, methodName.Name, instanceObject);
                }
            }
        }

        // ExecuteFile executes the file in a new module scope and stores the
        // scope on Globals, using either the provided name, globalVar, or the
        // file's base name.  This function returns the module scope.
        //
        public ExpandoObject ExecuteFile(string filename)
        {
            ArgumentNullException.ThrowIfNull(filename);
            return ExecuteFile(filename, null);
        }

        public ExpandoObject ExecuteFile(string filename, string? globalVar)
        {
            ArgumentNullException.ThrowIfNull(filename);
            var moduleNamespace = CreateNamespace();
            ExecuteFileInScope(filename, moduleNamespace);

            globalVar = globalVar ?? Path.GetFileNameWithoutExtension(filename);
            DynamicObjectHelpers.SetMember(Globals, globalVar, moduleNamespace);

            return moduleNamespace;
        }

        // ExecuteFileInScope executes the file in the given module scope.  This
        // does NOT store the module scope on Globals.  This function returns
        // nothing.
        //
        public void ExecuteFileInScope(string filename, ExpandoObject moduleNamespace)
        {
            ArgumentNullException.ThrowIfNull(filename);
            ArgumentNullException.ThrowIfNull(moduleNamespace);
            using var reader = new StreamReader(filename);
            // Simple way to convey script rundir for RuntimeHelpes.CrispyImport
            // to load .arity files.
            DynamicObjectHelpers.SetMember(moduleNamespace, "__file__", Path.GetFullPath(filename));
            SeedModuleNamespace(moduleNamespace);
            var asts = new Parser(new Tokenizer(reader)).ParseFile();
            var context = new Context(
                null,
                filename,
                this,
                Expression.Parameter(typeof(CrispyRuntime), "arityRuntime"),
                Expression.Parameter(typeof(ExpandoObject), "fileModule"),
                _instanceObjects);

            var body = new List<Expression>();
            foreach (var e in asts)
            {
                body.Add(e.Eval(context));
            }
            var moduleFun = Expression.Lambda<Action<CrispyRuntime, ExpandoObject>>(
                MakeBody(context, body),
                context.RuntimeExpr,
                context.ModuleExpr
            );
            var d = moduleFun.Compile();
            d(this, moduleNamespace);
        }

        // Execute a single expression parsed from string in the provided module
        // scope and returns the resulting value.
        //
        public object ExecuteExpr(string text, ExpandoObject moduleNamespace)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(moduleNamespace);
            SeedModuleNamespace(moduleNamespace);
            var t = new Tokenizer(new StringReader(text));
            var ast = new Parser(t).Parse();
            var context = new Context(
                              null,
                              "__snippet__",
                              this,
                              Expression.Parameter(typeof(CrispyRuntime), "arityRuntime"),
                              Expression.Parameter(typeof(ExpandoObject), "fileModule"),
                              _instanceObjects);

            List<Expression> body = [Expression.Convert(ast.Eval(context), typeof(object))];

            var moduleFunction = Expression.Lambda<Func<CrispyRuntime, ExpandoObject, object>>(
                MakeBody(context, body),
                context.RuntimeExpr,
                context.ModuleExpr
            );
            var d = moduleFunction.Compile();
            return d(this, moduleNamespace);
        }

        private void SeedModuleNamespace(ExpandoObject moduleNamespace)
        {
            var module = (IDictionary<string, object?>)moduleNamespace;
            foreach (var entry in (IDictionary<string, object?>)Globals)
            {
                if (!module.ContainsKey(entry.Key))
                {
                    module[entry.Key] = entry.Value;
                }
            }
        }

        private static BlockExpression MakeBody(Context context, IEnumerable<Expression> body)
        {
            return context.Variables.Count > 0
                ? Expression.Block(
                    [.. context.Variables.Values],
                    body
                )
                : Expression.Block(body);
        }
        public static ExpandoObject CreateNamespace()
        {
            return new ExpandoObject();
        }

        /////////////////////////
        // Canonicalizing Binders
        /////////////////////////

        // We need to canonicalize binders so that we can share L2 dynamic
        // dispatch caching across common call sites.  Every call site with the
        // same operation and same metadata on their binders should return the
        // same rules whenever presented with the same kinds of inputs.  The
        // DLR saves the L2 cache on the binder instance.  If one site somewhere
        // produces a rule, another call site performing the same operation with
        // the same metadata could get the L2 cached rule rather than computing
        // it again.  For this to work, we need to place the same binder instance
        // on those functionally equivalent call sites.

        private readonly Dictionary<string, CrispyGetMemberBinder> _getMemberBinders = [];

        internal CrispyGetMemberBinder GetGetMemberBinder(string name)
        {
            lock (_getMemberBinders)
            {
                // Don't lower the name.  Crispy is case-preserving in the metadata
                // in case some DynamicMetaObject ignores ignoreCase.  This makes
                // some interop cases work, but the cost is that if a Crispy program
                // spells ".foo" and ".Foo" at different sites, they won't share rules.
                if (_getMemberBinders.TryGetValue(name, out var binder))
                    return binder;
                binder = new CrispyGetMemberBinder(name);
                _getMemberBinders[name] = binder;
                return binder;
            }
        }

        private readonly Dictionary<string, CrispySetMemberBinder> _setMemberBinders = [];

        internal CrispySetMemberBinder GetSetMemberBinder(string name)
        {
            lock (_setMemberBinders)
            {
                // Don't lower the name.  Crispy is case-preserving in the metadata
                // in case some DynamicMetaObject ignores ignoreCase.  This makes
                // some interop cases work, but the cost is that if a Crispy program
                // spells ".foo" and ".Foo" at different sites, they won't share rules.
                if (_setMemberBinders.TryGetValue(name, out var binder))
                    return binder;
                binder = new CrispySetMemberBinder(name);
                _setMemberBinders[name] = binder;
                return binder;
            }
        }

        private readonly Dictionary<CallInfo, CrispyInvokeBinder> _invokeBinders = [];

        internal CrispyInvokeBinder GetInvokeBinder(CallInfo info)
        {
            lock (_invokeBinders)
            {
                if (_invokeBinders.TryGetValue(info, out var binder))
                    return binder;
                binder = new CrispyInvokeBinder(info);
                _invokeBinders[info] = binder;
                return binder;
            }
        }

        private readonly Dictionary<InvokeMemberBinderKey, CrispyInvokeMemberBinder> _invokeMemberBinders = [];

        internal CrispyInvokeMemberBinder GetInvokeMemberBinder(InvokeMemberBinderKey info)
        {
            ArgumentNullException.ThrowIfNull(info);
            lock (_invokeMemberBinders)
            {
                if (_invokeMemberBinders.TryGetValue(info, out var binder))
                    return binder;
                binder = new CrispyInvokeMemberBinder(info.Name, info.Info);
                _invokeMemberBinders[info] = binder;
                return binder;
            }
        }

        private readonly Dictionary<CallInfo, CrispyCreateInstanceBinder> _createInstanceBinders = [];

        internal CrispyCreateInstanceBinder GetCreateInstanceBinder(CallInfo info)
        {
            lock (_createInstanceBinders)
            {
                if (_createInstanceBinders.TryGetValue(info, out var binder))
                    return binder;
                binder = new CrispyCreateInstanceBinder(info);
                _createInstanceBinders[info] = binder;
                return binder;
            }
        }

        private readonly Dictionary<ExpressionType, CrispyBinaryOperationBinder> _binaryOperationBinders = [];

        internal CrispyBinaryOperationBinder GetBinaryOperationBinder(ExpressionType op)
        {
            lock (_binaryOperationBinders)
            {
                if (_binaryOperationBinders.TryGetValue(op, out var binder))
                    return binder;
                binder = new CrispyBinaryOperationBinder(op);
                _binaryOperationBinders[op] = binder;
                return binder;
            }
        }

        private readonly Dictionary<ExpressionType, CrispyUnaryOperationBinder> _unaryOperationBinders = [];

        internal CrispyUnaryOperationBinder GetUnaryOperationBinder(ExpressionType op)
        {
            lock (_unaryOperationBinders)
            {
                if (_unaryOperationBinders.TryGetValue(op, out var binder))
                    return binder;
                binder = new CrispyUnaryOperationBinder(op);
                _unaryOperationBinders[op] = binder;
                return binder;
            }
        }
    }

}
