using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Crispy
{
    internal sealed class Context
    {
        // Need runtime for interning Symbol constants at code generation time.
        private CrispyRuntime? RuntimeValue { get; }
        private ParameterExpression? RuntimeExprValue { get; }
        private ParameterExpression? ModuleExprValue { get; }
        private IReadOnlyList<object>? InstanceObjectValues { get; }

        public Context(Context? parent, string name)
            : this(parent, name, null, null, null, null) { }

        public Context(Context? parent,
            string name,
            CrispyRuntime? runtime,
            ParameterExpression? runtimeParam,
            ParameterExpression? moduleParam,
            IReadOnlyList<object>? instanceObjects = null)
        {
            IsLoop = false;
            Parent = parent;
            Name = name;
            RuntimeValue = runtime;
            RuntimeExprValue = runtimeParam;
            ModuleExprValue = moduleParam;
            InstanceObjectValues = instanceObjects;

            Params = new Dictionary<string, ParameterExpression>(StringComparer.OrdinalIgnoreCase);
            Variables = new Dictionary<string, ParameterExpression>(StringComparer.OrdinalIgnoreCase);

            IsLambda = false;
        }

        public string Name { get; }

        public IReadOnlyList<object>? InstanceObjects => InstanceObjectValues ?? Parent?.InstanceObjects;

        public Context? Parent { get; }

        public ParameterExpression ModuleExpr
        {
            get
            {
                return ModuleExprValue ?? Parent?.ModuleExpr
                    ?? throw new InvalidOperationException("Module expression is not available in this scope.");
            }
        }

        public ParameterExpression RuntimeExpr
        {
            get
            {
                return RuntimeExprValue ?? Parent?.RuntimeExpr
                    ?? throw new InvalidOperationException("Runtime expression is not available in this scope.");
            }
        }

        public CrispyRuntime Runtime
        {
            get
            {
                return RuntimeValue ?? Parent?.Runtime
                    ?? throw new InvalidOperationException("Runtime is not available in this scope.");
            }
        }

        public bool IsModule { get { return ModuleExprValue != null; } }

        public bool IsLambda { get; set; }

        public LabelTarget? ReturnLabel { get; set; }

        public ParameterExpression? CaughtException { get; set; }

        public ParameterExpression? ActiveCaughtException
        {
            get
            {
                return CaughtException ?? Parent?.ActiveCaughtException;
            }
        }

        public Context CallableScope
        {
            get
            {
                var curScope = this;
                while (!curScope.IsLambda)
                {
                    curScope = curScope.Parent ?? throw new InvalidOperationException("Callable scope not found.");
                }
                return curScope;
            }
        }

        public bool HasCallableAncestor
        {
            get
            {
                var curScope = this;
                while (curScope != null)
                {
                    if (curScope.IsLambda)
                    {
                        return true;
                    }

                    curScope = curScope.Parent;
                }

                return false;
            }
        }

        public bool IsLoop { get; set; }

        public LabelTarget? LoopBreak { get; set; }

        public LabelTarget? LoopContinue { get; set; }

        // List of function parameters
        public Dictionary<string, ParameterExpression> Params { get; }

        // List of local variables
        public Dictionary<string, ParameterExpression> Variables { get; }

        public Context TopScope
        {
            get
            {
                return Parent == null ? this : Parent.TopScope;
            }
        }

        public Expression GetOrMakeLocal(string name)
        {
            return GetOrMakeLocal(name, typeof(object));
        }

        public ParameterExpression GetOrMakeLocal(string name, Type type)
        {
            ParameterExpression? parameter;
            if (Variables.TryGetValue(name, out parameter))
            {
                return parameter;
            }

            parameter = Expression.Variable(type, name);
            Variables[name] = parameter;
            return parameter;
        }

        public Expression GetOrMakeGlobal(string name)
        {
            return TopScope.GetOrMakeLocal(name);
        }

        public Expression? LookupName(string name)
        {
            return Params.TryGetValue(name, out var parameter)
                ? parameter
                : Variables.TryGetValue(name, out parameter)
                    ? parameter
                : Parent?.LookupName(name);
        }
    }
}
