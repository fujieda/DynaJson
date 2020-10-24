using System;
using System.Runtime.CompilerServices;
using System.Threading;

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

        private int _working;
        private int _count = -1;
        private KeyValuePair[] _list = new KeyValuePair[5];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Insert(Type key, T value)
        {
            if (Interlocked.Exchange(ref _working, 1) != 0)
                return value;
            _count++;
            if (_count >= _list.Length)
                Array.Resize(ref _list, _list.Length * 2);
            _list[_count] = new KeyValuePair(key, value);
            Interlocked.Exchange(ref _working, 0);
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