using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DynaJson
{
    internal static class ReflectiveOperation
    {
        private class ReflectionCache<T>
        {
            private readonly TypeDictionary<T> _cache = new TypeDictionary<T>();
            private readonly Func<Type, T> _operation;

            public ReflectionCache(Func<Type, T> operation)
            {
                _operation = operation;
            }

            public T Get(Type type)
            {
                return _cache.TryGetValue(type, out var value) ? value : _cache.Insert(type, _operation(type));
            }
        }

        public class Getter
        {
            public string Name;
            public GetterDelegate Invoke;
        }

        public class Setter
        {
            public string Name;
            public Type Type;
            public Action<object, object> Invoke;
        }

        private static readonly ReflectionCache<Getter[]> GetterListCache =
            new ReflectionCache<Getter[]>(CreateGetterList);

        public static Getter[] GetGetterList(Type type)
        {
            return GetterListCache.Get(type);
        }

        private static Getter[] CreateGetterList(Type type)
        {
            return (from prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                select new Getter
                {
                    Name = prop.Name,
                    Invoke = MakeGetter(prop)
                }).ToArray();
        }

        private static GetterDelegate MakeGetter(PropertyInfo prop)
        {
            var paramThis = Expression.Parameter(typeof(object));
            var paramRefObj = Expression.Parameter(typeof(InternalObject).MakeByRefType());
            var type = prop.PropertyType;
            // ReSharper disable once AssignNullToNotNullAttribute
            var propExp = Expression.Property(Expression.Convert(paramThis, prop.DeclaringType), prop);
            var numberField = FieldExp(paramRefObj, "Number");
            var typeField = FieldExp(paramRefObj, "Type");
            var stringField = FieldExp(paramRefObj, "String");
            Expression body = Expression.Convert(propExp, typeof(object));
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    body = ReturnNull(Expression.Assign(typeField,
                        Expression.Condition(Expression.Convert(propExp, typeof(bool)),
                            Expression.Constant(JsonType.True), Expression.Constant(JsonType.False))));
                    break;
                case TypeCode.Int32:
                    body = ReturnNull(Expression.Assign(numberField,
                        Expression.Convert(propExp, typeof(double))));
                    break;
                case TypeCode.Single:
                    body = ReturnNull(Expression.Assign(numberField,
                        Expression.Convert(propExp, typeof(double))));
                    break;
                case TypeCode.Double:
                    body = ReturnNull(Expression.Assign(numberField, propExp));
                    break;
                case TypeCode.String:
                    body = Expression.Condition(
                        Expression.ReferenceEqual(propExp, Expression.Constant(null)),
                        Expression.Constant(null),
                        Expression.Block(
                            Expression.Assign(typeField, Expression.Constant(JsonType.String)),
                            Expression.Assign(stringField, propExp),
                            Expression.Constant(null)));
                    break;
            }
            return Expression.Lambda<GetterDelegate>(body, paramThis, paramRefObj).Compile();
        }

        private static Expression FieldExp(Expression obj, string name)
        {
            return Expression.Field(obj, typeof(InternalObject).GetField(name));
        }

        private static Expression ReturnNull(Expression lambda)
        {
            return Expression.Block(lambda, Expression.Constant(null));
        }

        public delegate object GetterDelegate(object target, ref InternalObject obj);

        private static readonly ReflectionCache<ObjectCreator> ObjectCreatorCache =
            new ReflectionCache<ObjectCreator>(ObjectCreator.Create);

        public class ObjectCreator
        {
            public readonly Func<object> Creator;
            public readonly Setter[] Setters;

            public static ObjectCreator Create(Type type)
            {
                return new ObjectCreator(type);
            }

            private ObjectCreator(Type type)
            {
                Creator = CreateObjectCreator(type);
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                    return;
                Setters = CreateSetterList(type);
            }
        }

        public static ObjectCreator GetObjectCreator(Type type)
        {
            return ObjectCreatorCache.Get(type);
        }

        private static Setter[] CreateSetterList(Type type)
        {
            return (from prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                where prop.CanWrite
                select new Setter
                {
                    Name = prop.Name,
                    Type = prop.PropertyType,
                    Invoke = MakeSetter(prop)
                }).ToArray();
        }

        private static Action<object, object> MakeSetter(PropertyInfo prop)
        {
            var paramThis = Expression.Parameter(typeof(object));
            var paramObj = Expression.Parameter(typeof(object));
            return Expression.Lambda<Action<object, object>>(
                Expression.Assign(
                    // ReSharper disable once AssignNullToNotNullAttribute
                    Expression.Property(Expression.Convert(paramThis, prop.DeclaringType), prop),
                    Expression.Convert(paramObj, prop.PropertyType)),
                paramThis, paramObj).Compile();
        }

        private static Func<object> CreateObjectCreator(Type type)
        {
            return Expression.Lambda<Func<object>>(Expression.New(type)).Compile();
        }
    }
}