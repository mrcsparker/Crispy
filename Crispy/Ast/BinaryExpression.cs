using System.Linq.Expressions;
using System.ComponentModel;
using System;

namespace Crispy.Ast
{
    class BinaryExpression : NodeExpression
    {
        private readonly ExpressionType _binaryOperator;
        private readonly NodeExpression _left;
        private readonly NodeExpression _right;

        public BinaryExpression(ExpressionType binaryOperator, NodeExpression left, NodeExpression right)
        {
            _binaryOperator = binaryOperator;
            _left = left;
            _right = right;
        }

        protected internal override Expression Eval(Context scope)
        {

            var left = _left.Eval(scope);
            var right = _right.Eval(scope);

            ConvertIfNecessary(ref left, ref right);

            return Expression.Dynamic(
                scope.GetRuntime().GetBinaryOperationBinder(_binaryOperator),
                typeof (object),
                left,
                right
            );
        }

        private static void ConvertIfNecessary(ref Expression left, ref Expression right)
        {
            if (right.Type.IsAssignableFrom(left.Type) ||
                left.Type.IsAssignableFrom(right.Type))
            {
                return;
            }

            var leftConverter = TypeDescriptor.GetConverter(left.Type);
            if (leftConverter.CanConvertTo(right.Type))
            {
                left = Expression.Convert(left, right.Type);
            }
            else
            {
                var rightConverter = TypeDescriptor.GetConverter(right.Type);
                if (rightConverter.CanConvertTo(left.Type))
                {
                    right = Expression.Convert(right, left.Type);
                }
            }
        }
    }
}
