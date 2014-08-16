using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Crispy
{
    public class Context
    {
        private readonly Context _parent;
        private readonly string _name;

        // Need runtime for interning Symbol constants at code generation time.
        private readonly Crispy _runtime;
        private readonly ParameterExpression _runtimeParam;
        private readonly ParameterExpression _moduleParam;

        // Need IsLambda when support return to find tightest closing fun.
        private bool _isLambda;
        private LabelTarget _returnLabel;
        private LabelTarget _loopBreak;

        public Context(Context parent, string name)
            : this(parent, name, null, null, null) { }

        public Context(Context parent,
            string name,
            Crispy runtime,
            ParameterExpression runtimeParam,
            ParameterExpression moduleParam) {
            IsLoop = false;
            _parent = parent;
            _name = name;
            _runtime = runtime;
            _runtimeParam = runtimeParam;
            _moduleParam = moduleParam;

            Params = new Dictionary<string, ParameterExpression>();
            Variables = new Dictionary<string, ParameterExpression>();

            IsLambda = false;
        }

        public string Name { get { return _name;  } }

        public List<object> InstanceObjects { get; set; }

        public Context Parent { get { return _parent; } }

        public ParameterExpression ModuleExpr { get { return _moduleParam; } }

        public ParameterExpression RuntimeExpr { get { return _runtimeParam; } }

        public Crispy Runtime { get { return _runtime; } }

        public bool IsModule { get { return _moduleParam != null; } }

        public bool IsLambda {
            get { return _isLambda; }
            set { _isLambda = value; }
        }

        public LabelTarget ReturnLabel
        {
            get { return _returnLabel; }
            set { _returnLabel = value; }
        }

        public bool IsLoop { get; set; }

        public LabelTarget LoopBreak {
            get { return _loopBreak; }
            set { _loopBreak = value; }
        }

        public LabelTarget LoopContinue {
            get { return _loopBreak; }
            set { _loopBreak = value; }
        }

        // List of function parameters
        public Dictionary<string, ParameterExpression> Params { get; set; }

        // List of local variables
        public Dictionary<string, ParameterExpression> Variables { get; set; }

        public ParameterExpression GetModuleExpr() {
            var curScope = this;
            while (!curScope.IsModule) {
                curScope = curScope.Parent;
            }
            return curScope.ModuleExpr;
        }

        public Crispy GetRuntime()
        {
            var curScope = this;
            while (curScope.Runtime == null) {
                curScope = curScope.Parent;
            }
            return curScope.Runtime;
        }

        public LabelTarget GetReturnLabel()
        {
            var curScope = this;
            while (curScope.IsLambda == false)
            {
                curScope = curScope.Parent;
            }
            return curScope.ReturnLabel;
        }

        public Context TopScope
        {
            get
            {
                return _parent == null ? this : _parent.TopScope;
            }
        }

        public Expression GetOrMakeLocal(string name)
        {
            return GetOrMakeLocal(name, typeof(object));
        }

        public ParameterExpression GetOrMakeLocal(string name, Type type)
        {
            ParameterExpression parameter;
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

        public Expression LookupName(string name)
        {
            ParameterExpression var;
            if (Params.TryGetValue(name, out var))
            {
                return var;
            }

            if (Variables.TryGetValue(name, out var))
            {
                return var;
            }

            return _parent != null ? _parent.LookupName(name) : null;
        }
    }
}
