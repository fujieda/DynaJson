using System;
using System.Runtime.CompilerServices;

namespace DynaJson
{
    public class TypeDictionary<T>
    {
        private readonly struct KeyValuePair
        {
            public readonly Type Key;
            public readonly T Value;

            public KeyValuePair(Type key, T value)
            {
                Key = key;
                Value = value;
            }
        }

        private int _count = -1;
        private KeyValuePair[] _list = new KeyValuePair[5];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Insert(Type key, T value)
        {
            _count++;
            if (_count >= _list.Length)
                Array.Resize(ref _list, _list.Length * 2);
            _list[_count] = new KeyValuePair(key, value);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(Type key, out T value)
        {
            value = default;
            for (var i = _count - 1; i >= 0; i--)
            {
                if (_list[i].Key != key)
                    continue;
                value = _list[i].Value;
                return true;
            }
            return false;
        }
    }
}