﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Convert;
using static System.Globalization.CultureInfo;

namespace DynaJson
{
    public partial class JsonObject
    {
        private enum ConvertMode
        {
            Array,
            List,
            Object
        }

        private static InvalidCastException InvalidCastException(InternalObject obj, Type type)
        {
            return new InvalidCastException($"Unable to cast value of type {obj.Type} to type '{type.Name}'");
        }

        private static class ConvertTo
        {
            private static readonly Stack<Context> Stack = new Stack<Context>();

            internal static object Convert(InternalObject obj, Type type)
            {
                if (type == typeof(IEnumerable))
                    return ConvertToIEnumerable(obj);
                return ConvertToObject(obj, type);
            }

            [StructLayout(LayoutKind.Explicit)]
            private struct Context
            {
                [FieldOffset(0)]
                public ConvertMode Mode;
                [FieldOffset(8)]
                public ArrayEnumerator ArrayEnumerator;
                [FieldOffset(8)]
                public ListEnumerator ListEnumerator;
                [FieldOffset(8)]
                public SetterEnumerator SetterEnumerator;
            }

            private static object ConvertToObject(InternalObject obj, Type type)
            {
                Stack.Count = 0;
                var context = new Context();
                object result;

                Convert:
                if (type == typeof(object))
                {
                    result = ToValue(obj);
                    goto Return;
                }
                switch (obj.Type)
                {
                    case JsonType.Null:
                        result = ChangeType(ToValue(obj), type, InvariantCulture);
                        break;
                    case JsonType.True:
                    case JsonType.False:
                        result = type == typeof(bool)
                            ? obj.Type == JsonType.True
                            : ChangeType(ToValue(obj), type, InvariantCulture);
                        break;
                    case JsonType.String:
                        result = type == typeof(string) ? obj.String : ChangeType(ToValue(obj), type, InvariantCulture);
                        break;
                    case JsonType.Array:
                        Stack.Push(context);
                        if (!type.IsArray && !IsGenericList(type))
                            throw InvalidCastException(obj, type);
                        if (type.IsArray)
                        {
                            context = new Context
                            {
                                Mode = ConvertMode.Array,
                                ArrayEnumerator = new ArrayEnumerator(type, obj.Array)
                            };
                            goto ArrayNext;
                        }
                        else
                        {
                            context = new Context
                            {
                                Mode = ConvertMode.List,
                                ListEnumerator = new ListEnumerator(type, obj.Array)
                            };
                            goto ListNext;
                        }
                    case JsonType.Object:
                        if (type.IsArray)
                            throw InvalidCastException(obj, type);
                        Stack.Push(context);
                        context = new Context
                        {
                            Mode = ConvertMode.Object,
                            SetterEnumerator = new SetterEnumerator(type, obj.Dictionary)
                        };
                        goto ObjectNext;
                    default:
                        result = type == typeof(double)
                            ? obj.Number
                            : type == typeof(int)
                                ? (int)obj.Number
                                : type == typeof(float)
                                    ? (float)obj.Number
                                    : ChangeType(ToValue(obj), type, InvariantCulture);
                        break;
                }

                Return:
                if (Stack.Count == 0)
                    return result;
                switch (context.Mode)
                {
                    case ConvertMode.Array:
                        context.ArrayEnumerator.SetResult(result);
                        break;
                    case ConvertMode.List:
                        context.ListEnumerator.SetResult(result);
                        goto ListNext;
                    case ConvertMode.Object:
                        context.SetterEnumerator.SetResult(result);
                        goto ObjectNext;
                }
                ArrayNext:
                if (!context.ArrayEnumerator.TryNext(ref obj))
                {
                    result = context.ArrayEnumerator.DstObject;
                    context = Stack.Pop();
                    goto Return;
                }
                type = context.ArrayEnumerator.Element;
                goto Convert;
                ListNext:
                if (!context.ListEnumerator.TryNext(ref obj))
                {
                    result = context.ListEnumerator.DstObject;
                    context = Stack.Pop();
                    goto Return;
                }
                type = context.ListEnumerator.Element;
                goto Convert;
                ObjectNext:
                if (!context.SetterEnumerator.TryNext(ref type, ref obj))
                {
                    result = context.SetterEnumerator.DstObject;
                    context = Stack.Pop();
                    goto Return;
                }
                goto Convert;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsGenericList(Type type)
            {
                return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
            }

            private class ArrayEnumerator
            {
                private readonly JsonArray.Enumerator _enumerator;
                public readonly Type Element;
                public dynamic DstObject { get; }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public ArrayEnumerator(Type type, JsonArray array)
                {
                    Element = type.GetElementType();
                    DstObject = Array.CreateInstance(Element, array.Count);
                    _enumerator = array.GetEnumerator();
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool TryNext(ref InternalObject obj)
                {
                    if (!_enumerator.MoveNext())
                        return false;
                    obj = _enumerator.Current;
                    return true;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void SetResult(dynamic result)
                {
                    DstObject[_enumerator.Position] = result;
                }
            }

            private class ListEnumerator
            {
                private readonly JsonArray.Enumerator _enumerator;
                public readonly Type Element;
                public dynamic DstObject { get; }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public ListEnumerator(Type type, JsonArray array)
                {
                    DstObject = ReflectiveOperation.GetObjectCreator(type)();
                    _enumerator = array.GetEnumerator();
                    Element = type.GenericTypeArguments[0];
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool TryNext(ref InternalObject obj)
                {
                    if (!_enumerator.MoveNext())
                        return false;
                    obj = _enumerator.Current;
                    return true;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void SetResult(dynamic result)
                {
                    DstObject.Add(result);
                }
            }

            private class SetterEnumerator
            {
                private readonly ReflectiveOperation.Setter[] _setters;
                private readonly JsonDictionary _dict;
                private int _position = -1;
                private ReflectiveOperation.Setter _current;

                public object DstObject { get; }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public SetterEnumerator(Type type, JsonDictionary dict)
                {
                    DstObject = ReflectiveOperation.GetObjectCreator(type)();
                    _setters = ReflectiveOperation.GetSetterList(type);
                    _dict = dict;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool TryNext(ref Type type, ref InternalObject value)
                {
                    while (true)
                    {
                        _position++;
                        if (_position == _setters.Length)
                            return false;
                        _current = _setters[_position];
                        type = _current.Type;
                        if (_dict.TryGetValue(_current.Name, out value))
                            return true;
                    }
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void SetResult(object result)
                {
                    _current.Invoke(DstObject, result);
                }
            }
        }

        private static object ConvertToIEnumerable(InternalObject obj)
        {
            return obj.Type == JsonType.Array
                ? (object)obj.Array.GetEnumerator().GetEnumerable().Select(ToValue)
                : obj.Dictionary.GetEnumerator().GetEnumerable().ToDictionary(x => x.Key, x => ToValue(x.Value));
        }
    }
}