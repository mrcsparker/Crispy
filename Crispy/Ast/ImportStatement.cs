using System;
using System.Linq.Expressions;
using Crispy.Helpers;
using System.Collections.Generic;
using System.Reflection;

namespace Crispy.Ast
{
    sealed class ImportStatement : NodeExpression
    {
        private static readonly MethodInfo CrispyImportMethod =
            typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.CrispyImport)) ??
            throw new InvalidOperationException("RuntimeHelpers.CrispyImport was not found.");
        private static readonly MethodInfo ResolveImportMethod =
            typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.ResolveImport)) ??
            throw new InvalidOperationException("RuntimeHelpers.ResolveImport was not found.");

        private readonly List<string> _names = [];
        private readonly string? _nameAs;

        public ImportStatement(List<string> names, string? nameAs)
        {
            _names = names;
            _nameAs = nameAs;
        }

        protected internal override Expression Eval(Context scope)
        {
            if (scope.IsModule)
            {
                Expression nameAs = Expression.Constant(new[] { String.Empty });
                if (_nameAs != null)
                {
                    nameAs = Expression.Constant(new[] { _nameAs });
                }

                return Expression.Call(
                    CrispyImportMethod,
                    scope.RuntimeExpr,
                    scope.ModuleExpr,
                    Expression.Constant(_names.ToArray()),
                    nameAs
                );
            }

            var targetName = _nameAs ?? _names[^1];
            var local = scope.GetOrMakeLocal(targetName);
            var value = Expression.Call(
                ResolveImportMethod,
                scope.RuntimeExpr,
                scope.ModuleExpr,
                Expression.Constant(_names.ToArray()));

            return Expression.Assign(local, Expression.Convert(value, local.Type));
        }
    }
}
