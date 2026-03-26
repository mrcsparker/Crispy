using System;
using System.Dynamic;
using System.Linq.Expressions;

namespace Crispy
{
    internal sealed class TypeModel : IDynamicMetaObjectProvider
    {
        public Type ReflType { get; }

        public TypeModel(Type type)
        {
            ReflType = type;
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new TypeModelMetaObject(parameter, this);
        }
    }
}
