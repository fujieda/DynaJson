﻿using System;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DynaJson
{
    public partial class JsonObject
    {
        private class ConvertFrom
        {
            [StructLayout(LayoutKind.Explicit)]
            private struct Context
            {
                [FieldOffset(0)]
                public ConvertMode Mode;
                [FieldOffset(8)]
                public ArrayEnumerator ArrayEnumerator;
                [FieldOffset(8)]
                public GetterEnumerator GetterEnumerator;
                [FieldOffset(8)]
                public DictionaryEnumerator DictionaryEnumerator;
            }

            private readonly Stack<Context> _stack = new Stack<Context>();

            public static InternalObject Convert(object value)
            {
                return new ConvertFrom().ConvertInternal(value);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private InternalObject ConvertInternal(object value)
            {
                var context = new Context();
                var result = new InternalObject();

                Convert:
                if (value == null)
                {
                    result.Type = JsonType.Null;
                    goto Return;
                }
                var type = value.GetType();
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Empty:
                    case TypeCode.DBNull:
                        result.Type = JsonType.Null;
                        break;
                    case TypeCode.Boolean:
                        result.Type = (bool)value ? JsonType.True : JsonType.False;
                        break;
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Decimal:
                        result.Number = ConvertToDouble(value);
                        break;
                    case TypeCode.Int32:
                        result.Number = (int)value;
                        break;
                    case TypeCode.Single:
                        result.Number = (float)value;
                        break;
                    case TypeCode.Double:
                        result.Number = (double)value;
                        break;
                    case TypeCode.Char:
                    case TypeCode.DateTime:
                        result.Type = JsonType.String;
                        result.String = ConvertToString(value);
                        break;
                    case TypeCode.String:
                        result.Type = JsonType.String;
                        result.String = (string)value;
                        break;
                    case TypeCode.Object:
                        if (typeof(IDictionary).IsAssignableFrom(type))
                        {
                            _stack.Push(context);
                            context = new Context
                            {
                                Mode = ConvertMode.Dictionary,
                                DictionaryEnumerator = new DictionaryEnumerator(value)
                            };
                            goto DictionaryNext;
                        }
                        if (typeof(IEnumerable).IsAssignableFrom(type)) // Can convert to array
                        {
                            _stack.Push(context);
                            context = new Context
                            {
                                Mode = ConvertMode.Array,
                                ArrayEnumerator = new ArrayEnumerator(value)
                            };
                            goto ArrayNext;
                        }
                        if (value is JsonObject obj)
                        {
                            result = obj._data;
                            goto Return;
                        }
                        _stack.Push(context);
                        var v1 = value;
                        context = new Context
                        {
                            Mode = ConvertMode.Object,
                            GetterEnumerator = new GetterEnumerator(v1),
                        };
                        goto ObjectNext;
                }

                Return:
                if (_stack.Count == 0)
                    return result;
                if (context.Mode == ConvertMode.Array)
                {
                    context.ArrayEnumerator.SetResult(result);
                    goto ArrayNext;
                }
                if (context.Mode == ConvertMode.Dictionary)
                {
                    context.DictionaryEnumerator.SetResult(result);
                    goto DictionaryNext;
                }
                context.GetterEnumerator.SetResult(result);

                ObjectNext:
                if (context.GetterEnumerator.TryNext(ref value, ref result))
                    goto Convert;
                result.Type = JsonType.Object;
                result.Dictionary = context.GetterEnumerator.DstDictionary;
                context = _stack.Pop();
                goto Return;

                ArrayNext:
                if (context.ArrayEnumerator.TryNext(ref value))
                    goto Convert;
                result.Type = JsonType.Array;
                result.Array = context.ArrayEnumerator.DstArray;
                context = _stack.Pop();
                goto Return;

                DictionaryNext:
                if (context.DictionaryEnumerator.TryNext(ref value))
                    goto Convert;
                result.Type = JsonType.Object;
                result.Dictionary = context.DictionaryEnumerator.DstDictionary;
                context = _stack.Pop();
                goto Return;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double ConvertToDouble(object value)
            {
                return (double)System.Convert.ChangeType(value, typeof(double), CultureInfo.InvariantCulture);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static string ConvertToString(object value)
            {
                return (string)System.Convert.ChangeType(value, typeof(string), CultureInfo.InvariantCulture);
            }

            private class ArrayEnumerator
            {
                private readonly IEnumerator _enumerator;

                public readonly JsonArray DstArray = new JsonArray();

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public ArrayEnumerator(object value)
                {
                    _enumerator = ((IEnumerable)value).GetEnumerator();
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool TryNext(ref object value)
                {
                    if (!_enumerator.MoveNext())
                        return false;
                    value = _enumerator.Current;
                    return true;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void SetResult(InternalObject result)
                {
                    DstArray.Add(result);
                }
            }

            private class GetterEnumerator
            {
                private readonly ReflectiveOperation.Getter[] _getters;
                private readonly object _target;
                private int _position = -1;
                private string _name;

                public readonly JsonDictionary DstDictionary = new JsonDictionary();

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public GetterEnumerator(object target)
                {
                    _target = target;
                    _getters = ReflectiveOperation.GetGetterList(_target.GetType());
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool TryNext(ref object value, ref InternalObject result)
                {
                    while (true)
                    {
                        _position++;
                        if (_position == _getters.Length)
                            return false;
                        result.Type = JsonType.Null;
                        value = _getters[_position].Invoke(_target, ref result);
                        _name = _getters[_position].Name;
                        if (result.Type == JsonType.Null)
                            return true;
                        DstDictionary[_name] = result;
                    }
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void SetResult(InternalObject result)
                {
                    DstDictionary[_name] = result;
                }
            }

            private class DictionaryEnumerator
            {
                private readonly IDictionaryEnumerator _enumerator;
                private string _key;

                public readonly JsonDictionary DstDictionary = new JsonDictionary();

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public DictionaryEnumerator(object value)
                {
                    _enumerator = ((IDictionary)value).GetEnumerator();
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool TryNext(ref object value)
                {
                    if (!_enumerator.MoveNext())
                        return false;
                    _key = (string)_enumerator.Key;
                    value = _enumerator.Value;
                    return true;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void SetResult(InternalObject result)
                {
                    DstDictionary[_key] = result;
                }
            }
        }
    }
}