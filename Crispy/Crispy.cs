using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Crispy.Parsing;
using System.Dynamic;
using System.Reflection;
using System.Collections.Generic;
using Crispy.Helpers;
using Crispy.Binders;

namespace Crispy
{
    public class Crispy
    {

        private readonly Assembly[] _assemblies;
        private readonly ExpandoObject _globals = new ExpandoObject();
        private readonly List<object> _instanceObjects = new List<object>();

        public Crispy(Assembly[] assms)
        {
            _assemblies = assms;
            AddAssemblyNamesAndTypes();
        }

        public Crispy(Assembly[] assms, object[] instanceObjects)
        {
            _assemblies = assms;
            //AddAssemblyNamesAndTypes();
            _instanceObjects = instanceObjects.ToList();
            AddInstanceObjectNamesAndTypes();

        }

        // _addNamespacesAndTypes builds a tree of ExpandoObjects representing
        // .NET namespaces, with TypeModel objects at the leaves.  Though Crispy is
        // case-insensitive, we store the names as they appear in .NET reflection
        // in case our globals object or a namespace object gets passed as an IDO
        // to another language or library, where they may be looking for names
        // case-sensitively using EO's default lookup.
        //
        public void AddAssemblyNamesAndTypes()
        {
            foreach (var assm in _assemblies)
            {
                foreach (var typ in assm.GetExportedTypes())
                {
                    string[] names = typ.FullName.Split('.');
                    var table = _globals;
                    for (int i = 0; i < names.Length - 1; i++)
                    {
                        string name = names[i].ToLower();
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

        public void AddInstanceObjectNamesAndTypes()
        {
            foreach (var instanceObject in _instanceObjects)
            {
                foreach (var methodName in instanceObject.GetType().GetMethods())
                {
                    var table = _globals;
                    if (DynamicObjectHelpers.HasMember(table, instanceObject.GetType().Name))
                    {
                        table = (ExpandoObject)(DynamicObjectHelpers.GetMember(table, instanceObject.GetType().Name));
                    } else
                    {
                        var tmp = new ExpandoObject();
                        DynamicObjectHelpers.SetMember(table, instanceObject.GetType().Name, tmp);
                        table = tmp;
                    }

                    DynamicObjectHelpers.SetMember(table, methodName.Name, instanceObject);
                }
            }
        }

        // ExecuteFile executes the file in a new module scope and stores the
        // scope on Globals, using either the provided name, globalVar, or the
        // file's base name.  This function returns the module scope.
        //
        public ExpandoObject ExecuteFile(string filename)
        {
            return ExecuteFile(filename, null);
        }

        public ExpandoObject ExecuteFile(string filename, string globalVar)
        {
            var moduleNamespace = CreateNamespace();
            ExecuteFileInScope(filename, moduleNamespace);

            globalVar = globalVar ?? Path.GetFileNameWithoutExtension(filename);
            DynamicObjectHelpers.SetMember(_globals, globalVar, moduleNamespace);

            return moduleNamespace;
        }

        // ExecuteFileInScope executes the file in the given module scope.  This
        // does NOT store the module scope on Globals.  This function returns
        // nothing.
        //
        public void ExecuteFileInScope(string filename, ExpandoObject moduleNamespace)
        {
            var f = new StreamReader(filename);
            // Simple way to convey script rundir for RuntimeHelpes.CrispyImport
            // to load .arity files.
            DynamicObjectHelpers.SetMember(moduleNamespace, "__file__", Path.GetFullPath(filename));
            try
            {
                var asts = new Parser(new Tokenizer(f)).ParseFile();
                var context = new Context(
                    null,
                    filename,
                    this,
                    Expression.Parameter(typeof(Crispy), "arityRuntime"),
                    Expression.Parameter(typeof(ExpandoObject), "fileModule")
                ) {
                    InstanceObjects = _instanceObjects
                };

                var body = new List<Expression>();
                foreach (var e in asts)
                {
                    body.Add(e.Eval(context));
                }
                var moduleFun = Expression.Lambda<Action<Crispy, ExpandoObject>>(
                    MakeBody(context, body),
                    context.RuntimeExpr,
                    context.ModuleExpr
                );
                var d = moduleFun.Compile();
                d(this, moduleNamespace);
            }
            finally
            {
                f.Close();
            }
        }

        // Execute a single expression parsed from string in the provided module
        // scope and returns the resulting value.
        //
        public object ExecuteExpr(string text, ExpandoObject moduleNamespace)
        {
            var t = new Tokenizer(new StringReader(text));
            var ast = new Parser(t).Parse();
            var context = new Context(
                              null,
                              "__snippet__",
                              this,
                              Expression.Parameter(typeof(Crispy), "arityRuntime"),
                              Expression.Parameter(typeof(ExpandoObject), "fileModule")
                          )
            {
                InstanceObjects = _instanceObjects
            };

            List<Expression> body = new List<Expression>();

            body.Add(Expression.Convert(ast.Eval(context), typeof(object)));

            var moduleFunction = Expression.Lambda<Func<Crispy, ExpandoObject, object>>(
                MakeBody(context, body),
                context.RuntimeExpr,
                context.ModuleExpr
            );
            var d = moduleFunction.Compile();
            return d(this, moduleNamespace);
        }

        private static Expression MakeBody(Context context, IEnumerable<Expression> body)
        {
            if (context.Variables.Count > 0)
            {
                return Expression.Block(
                    context.Variables.Select(name => name.Value).ToArray(),
                    body
                );
            }
            return Expression.Block(body);
        }


        public ExpandoObject Globals { get { return _globals; } }

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

        private readonly Dictionary<string, CrispyGetMemberBinder> _getMemberBinders = new Dictionary<string, CrispyGetMemberBinder>();

        public CrispyGetMemberBinder GetGetMemberBinder(string name)
        {
            lock (_getMemberBinders)
            {
                // Don't lower the name.  Crispy is case-preserving in the metadata
                // in case some DynamicMetaObject ignores ignoreCase.  This makes
                // some interop cases work, but the cost is that if a Crispy program
                // spells ".foo" and ".Foo" at different sites, they won't share rules.
                if (_getMemberBinders.ContainsKey(name))
                    return _getMemberBinders[name];
                var b = new CrispyGetMemberBinder(name);
                _getMemberBinders[name] = b;
                return b;
            }
        }

        private readonly Dictionary<string, CrispySetMemberBinder> _setMemberBinders = new Dictionary<string, CrispySetMemberBinder>();

        public CrispySetMemberBinder GetSetMemberBinder(string name)
        {
            lock (_setMemberBinders)
            {
                // Don't lower the name.  Crispy is case-preserving in the metadata
                // in case some DynamicMetaObject ignores ignoreCase.  This makes
                // some interop cases work, but the cost is that if a Crispy program
                // spells ".foo" and ".Foo" at different sites, they won't share rules.
                if (_setMemberBinders.ContainsKey(name))
                    return _setMemberBinders[name];
                var b = new CrispySetMemberBinder(name);
                _setMemberBinders[name] = b;
                return b;
            }
        }

        private readonly Dictionary<CallInfo, CrispyInvokeBinder> _invokeBinders = new Dictionary<CallInfo, CrispyInvokeBinder>();

        public CrispyInvokeBinder GetInvokeBinder(CallInfo info)
        {
            lock (_invokeBinders)
            {
                if (_invokeBinders.ContainsKey(info))
                    return _invokeBinders[info];
                var b = new CrispyInvokeBinder(info);
                _invokeBinders[info] = b;
                return b;
            }
        }

        private readonly Dictionary<InvokeMemberBinderKey, CrispyInvokeMemberBinder> _invokeMemberBinders = new Dictionary<InvokeMemberBinderKey, CrispyInvokeMemberBinder>();

        public CrispyInvokeMemberBinder GetInvokeMemberBinder(InvokeMemberBinderKey info)
        {
            lock (_invokeMemberBinders)
            {
                if (_invokeMemberBinders.ContainsKey(info))
                    return _invokeMemberBinders[info];
                var b = new CrispyInvokeMemberBinder(info.Name, info.Info);
                _invokeMemberBinders[info] = b;
                return b;
            }
        }

        private readonly Dictionary<CallInfo, CrispyCreateInstanceBinder> _createInstanceBinders = new Dictionary<CallInfo, CrispyCreateInstanceBinder>();

        public CrispyCreateInstanceBinder GetCreateInstanceBinder(CallInfo info)
        {
            lock (_createInstanceBinders)
            {
                if (_createInstanceBinders.ContainsKey(info))
                    return _createInstanceBinders[info];
                var b = new CrispyCreateInstanceBinder(info);
                _createInstanceBinders[info] = b;
                return b;
            }
        }

        private readonly Dictionary<ExpressionType, CrispyBinaryOperationBinder> _binaryOperationBinders = new Dictionary<ExpressionType, CrispyBinaryOperationBinder>();

        public CrispyBinaryOperationBinder GetBinaryOperationBinder(ExpressionType op)
        {
            lock (_binaryOperationBinders)
            {
                if (_binaryOperationBinders.ContainsKey(op))
                    return _binaryOperationBinders[op];
                var b = new CrispyBinaryOperationBinder(op);
                _binaryOperationBinders[op] = b;
                return b;
            }
        }

        private readonly Dictionary<ExpressionType, CrispyUnaryOperationBinder> _unaryOperationBinders = new Dictionary<ExpressionType, CrispyUnaryOperationBinder>();

        public CrispyUnaryOperationBinder GetUnaryOperationBinder(ExpressionType op)
        {
            lock (_unaryOperationBinders)
            {
                if (_unaryOperationBinders.ContainsKey(op))
                    return _unaryOperationBinders[op];
                var b = new CrispyUnaryOperationBinder(op);
                _unaryOperationBinders[op] = b;
                return b;
            }
        }
    }

}
