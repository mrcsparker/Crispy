using System;
using System.Linq.Expressions;
using Crispy.Helpers;
using System.Collections.Generic;

namespace Crispy.Ast
{
    class ImportStatement : NodeExpression
    {
        private readonly List<string> _names = new List<string>();
        private readonly string _nameAs;

        public ImportStatement(List<string> names, string nameAs)
        {
            _names = names;
            _nameAs = nameAs;
        }

        protected internal override Expression Eval(Context scope)
        {
            if (!scope.IsModule)
            {
                throw new InvalidOperationException("Import can't be nested.");
            }

            Expression nameAs = Expression.Constant(new [] { String.Empty });
            if (_nameAs != null)
            {
                nameAs = Expression.Constant(new [] { _nameAs });
            }

            return Expression.Call(
                typeof(RuntimeHelpers).GetMethod("CrispyImport"),
                scope.RuntimeExpr,
                scope.ModuleExpr,
                Expression.Constant(_names.ToArray()),
                nameAs 
            );
        }
    }
}
