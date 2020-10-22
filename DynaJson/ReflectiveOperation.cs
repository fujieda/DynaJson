using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

// ReSharper disable AssignNullToNotNullAttribute

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
            public Action<object, InternalObject> DirectInvoke;
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
            public readonly Type Element;

            public static ObjectCreator Create(Type type)
            {
                return new ObjectCreator(type);
            }

            private ObjectCreator(Type type)
            {
                Creator = CreateObjectCreator(type);
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Element = type.GenericTypeArguments[0];
                    return;
                }
                Setters = CreateSetterList(type);
            }
        }

        public static ObjectCreator GetObjectCreator(Type type)
        {
            return ObjectCreatorCache.Get(type);
        }

        private static Setter[] CreateSetterList(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => prop.CanWrite).Select(
                prop =>
                {
                    var setter = new Setter
                    {
                        Name = prop.Name,
                        Type = prop.PropertyType,
                    };
                    if (prop.PropertyType == typeof(bool))
                    {
                        setter.DirectInvoke = MakeDirectSetter(prop, ExpGen.Bool);
                        return setter;
                    }
                    if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(float) ||
                        prop.PropertyType == typeof(double))
                    {
                        setter.DirectInvoke = MakeDirectSetter(prop, ExpGen.Number);
                        return setter;
                    }
                    if (prop.PropertyType == typeof(string))
                    {
                        setter.DirectInvoke = MakeDirectSetter(prop, ExpGen.String);
                        return setter;
                    }
                    setter.Invoke = MakeSetter(prop);
                    return setter;
                }).ToArray();
        }

        private static Action<object, object> MakeSetter(PropertyInfo prop)
        {
            var paramThis = Expression.Parameter(typeof(object));
            var paramObj = Expression.Parameter(typeof(object));
            return Expression.Lambda<Action<object, object>>(
                Expression.Assign(
                    Expression.Property(Expression.Convert(paramThis, prop.DeclaringType), prop),
                    Expression.Convert(paramObj, prop.PropertyType)),
                paramThis, paramObj).Compile();
        }

        private interface IExpGen
        {
            Expression GetValue(ParameterExpression paramObj, Type type);
            Expression CheckType(ParameterExpression paramObj);
        }

        private static class ExpGen
        {
            public static readonly BoolExpGen Bool = new BoolExpGen();
            public static readonly NumberExpGen Number = new NumberExpGen();
            public static readonly StringExpGen String = new StringExpGen();
        }

        private class BoolExpGen : IExpGen
        {
            public Expression GetValue(ParameterExpression paramObj, Type type)
            {
                var fieldType = FieldExp(paramObj, "Type");
                return Expression.Condition(
                    Expression.Equal(fieldType, Expression.Constant(JsonType.True)),
                    Expression.Constant(true), Expression.Constant(false));
            }

            public Expression CheckType(ParameterExpression paramObj)
            {
                var fieldType = FieldExp(paramObj, "Type");
                return Expression.Or(Expression.Equal(fieldType, Expression.Constant(JsonType.True)),
                    Expression.Equal(fieldType, Expression.Constant(JsonType.False)));
            }
        }

        private class NumberExpGen : IExpGen
        {
            public Expression GetValue(ParameterExpression paramObj, Type type)
            {
                var fieldNumber = FieldExp(paramObj, "Number");
                return Expression.Convert(fieldNumber, type);
            }

            public Expression CheckType(ParameterExpression paramObj)
            {
                var fieldType = FieldExp(paramObj, "Type");
                var sub = Expression.Variable(typeof(uint));
                return Expression.Not(Expression.Block(new[] {sub},
                    Expression.Assign(sub,
                        Expression.Subtract(Expression.Convert(fieldType, typeof(uint)),
                            Expression.Constant(0xfff80000))),
                    Expression.And(Expression.LessThanOrEqual(sub, Expression.Constant(6u)),
                        Expression.GreaterThanOrEqual(sub, Expression.Constant(1u)))));
            }
        }

        private class StringExpGen : IExpGen
        {
            public Expression GetValue(ParameterExpression paramObj, Type type)
            {
                return FieldExp(paramObj, "String");
            }

            public Expression CheckType(ParameterExpression paramObj)
            {
                var fieldType = FieldExp(paramObj, "Type");
                return Expression.Equal(fieldType, Expression.Constant(JsonType.String));
            }
        }

        private static readonly MethodInfo ChangeTypeMethod =
            typeof(Convert).GetMethod("ChangeType", BindingFlags.Static | BindingFlags.Public, null,
                CallingConventions.Any, new[] {typeof(object), typeof(Type), typeof(IFormatProvider)}, null);

        private static readonly MethodInfo ToValueMethod =
            typeof(JsonObject).GetMethod("ToValue", BindingFlags.Static | BindingFlags.NonPublic);

        private static Expression ChangeTypeExp(ParameterExpression paramObj, Type type)
        {
            return Expression.Convert(
                Expression.Call(ChangeTypeMethod, Expression.Call(ToValueMethod, paramObj), Expression.Constant(type),
                    Expression.Constant(CultureInfo.InvariantCulture)), type);
        }

        private static Action<object, InternalObject> MakeDirectSetter(PropertyInfo prop, IExpGen expGen)
        {
            var paramThis = Expression.Parameter(typeof(object));
            var paramObj = Expression.Parameter(typeof(InternalObject));
            return Expression.Lambda<Action<object, InternalObject>>(
                Expression.Assign(
                    Expression.Property(Expression.Convert(paramThis, prop.DeclaringType), prop),
                    Expression.Condition(expGen.CheckType(paramObj),
                        expGen.GetValue(paramObj, prop.PropertyType),
                        ChangeTypeExp(paramObj, prop.PropertyType))),
                paramThis, paramObj).Compile();
        }

        private static Func<object> CreateObjectCreator(Type type)
        {
            return Expression.Lambda<Func<object>>(Expression.New(type)).Compile();
        }
    }
}