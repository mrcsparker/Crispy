using System;
using System.Linq.Expressions;
using System.Dynamic;

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
