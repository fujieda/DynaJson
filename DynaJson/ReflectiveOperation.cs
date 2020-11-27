using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

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
            return MakeGetters(type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                .Concat(MakeGetters(type.GetFields(BindingFlags.Public | BindingFlags.Instance))).ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<Getter> MakeGetters(IEnumerable<PropertyInfo> properties)
        {
            foreach (var property in properties)
            {
                var paramThis = Expression.Parameter(typeof(object));
                var paramRefObj = Expression.Parameter(typeof(InternalObject).MakeByRefType());
                var memberExp = Expression.Property(Expression.Convert(paramThis, property.DeclaringType), property);
                var assignExp = AssignResult(memberExp, paramRefObj, property.PropertyType);
                var body = assignExp == null
                    ? Expression.Convert(memberExp, typeof(object))
                    : (Expression)Expression.Block(assignExp, Expression.Constant(null));
                yield return new Getter
                {
                    Name = property.Name,
                    Invoke = Expression.Lambda<GetterDelegate>(body, paramThis, paramRefObj).Compile()
                };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<Getter> MakeGetters(IEnumerable<FieldInfo> fields)
        {
            foreach (var field in fields)
            {
                var paramThis = Expression.Parameter(typeof(object));
                var paramRefObj = Expression.Parameter(typeof(InternalObject).MakeByRefType());
                var memberExp = Expression.Field(Expression.Convert(paramThis, field.DeclaringType), field);
                var assignExp = AssignResult(memberExp, paramRefObj, field.FieldType);
                var body = assignExp == null
                    ? Expression.Convert(memberExp, typeof(object))
                    : (Expression)Expression.Block(assignExp, Expression.Constant(null));
                yield return new Getter
                {
                    Name = field.Name,
                    Invoke = Expression.Lambda<GetterDelegate>(body, paramThis, paramRefObj).Compile()
                };
            }
        }

        private static Expression AssignResult(MemberExpression memberExp, ParameterExpression result, Type type)
        {
            var numberField = FieldExp(result, "Number");
            var typeField = FieldExp(result, "Type");
            var stringField = FieldExp(result, "String");
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return Expression.Assign(typeField,
                        Expression.Condition(Expression.Convert(memberExp, typeof(bool)),
                            Expression.Constant(JsonType.True), Expression.Constant(JsonType.False)));
                case TypeCode.Int32:
                case TypeCode.Single:
                    return Expression.Assign(numberField, Expression.Convert(memberExp, typeof(double)));
                case TypeCode.Double:
                    return Expression.Assign(numberField, memberExp);
                case TypeCode.String:
                    return Expression.IfThen(
                        Expression.ReferenceNotEqual(memberExp, Expression.Constant(null)),
                        Expression.Block(
                            Expression.Assign(typeField, Expression.Constant(JsonType.String)),
                            Expression.Assign(stringField, memberExp)));
                default:
                    return null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Expression FieldExp(Expression obj, string name)
        {
            return Expression.Field(obj, typeof(InternalObject).GetField(name));
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
            return MakeSetters(type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                .Concat(MakeSetters(type.GetFields(BindingFlags.Public | BindingFlags.Instance))).ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<Setter> MakeSetters(IEnumerable<PropertyInfo> properties)
        {
            return properties.Where(prop => prop.CanWrite).Select(
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
                });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<Setter> MakeSetters(IEnumerable<FieldInfo> fields)
        {
            return fields.Select(
                field =>
                {
                    var setter = new Setter
                    {
                        Name = field.Name,
                        Type = field.FieldType,
                    };
                    if (field.FieldType == typeof(bool))
                    {
                        setter.DirectInvoke = MakeDirectSetter(field, ExpGen.Bool);
                        return setter;
                    }
                    if (field.FieldType == typeof(int) || field.FieldType == typeof(float) ||
                        field.FieldType == typeof(double))
                    {
                        setter.DirectInvoke = MakeDirectSetter(field, ExpGen.Number);
                        return setter;
                    }
                    if (field.FieldType == typeof(string))
                    {
                        setter.DirectInvoke = MakeDirectSetter(field, ExpGen.String);
                        return setter;
                    }
                    setter.Invoke = MakeSetter(field);
                    return setter;
                });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Action<object, object> MakeSetter(FieldInfo prop)
        {
            var paramThis = Expression.Parameter(typeof(object));
            var paramObj = Expression.Parameter(typeof(object));
            return Expression.Lambda<Action<object, object>>(
                Expression.Assign(
                    Expression.Field(Expression.Convert(paramThis, prop.DeclaringType), prop),
                    Expression.Convert(paramObj, prop.FieldType)),
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        private static Action<object, InternalObject> MakeDirectSetter(FieldInfo field, IExpGen expGen)
        {
            var paramThis = Expression.Parameter(typeof(object));
            var paramObj = Expression.Parameter(typeof(InternalObject));
            return Expression.Lambda<Action<object, InternalObject>>(
                Expression.Assign(
                    Expression.Field(Expression.Convert(paramThis, field.DeclaringType), field),
                    Expression.Condition(expGen.CheckType(paramObj),
                        expGen.GetValue(paramObj, field.FieldType),
                        ChangeTypeExp(paramObj, field.FieldType))),
                paramThis, paramObj).Compile();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<object> CreateObjectCreator(Type type)
        {
            return Expression.Lambda<Func<object>>(Expression.New(type)).Compile();
        }
    }
}