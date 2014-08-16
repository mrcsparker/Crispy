using System;
using System.Linq.Expressions;
using System.Dynamic;

namespace Crispy
{
    public class TypeModel : IDynamicMetaObjectProvider {
        private readonly Type _reflType;

        public TypeModel(Type type) {
            _reflType = type;
        }

        public Type ReflType { get { return _reflType; } }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) {
            return new TypeModelMetaObject(parameter, this);
        }
    }
}
