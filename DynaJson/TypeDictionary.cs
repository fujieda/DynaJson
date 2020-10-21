using System;
using System.Runtime.CompilerServices;

namespace DynaJson
{
    public class TypeDictionary<T>
    {
        private int _count = - 1;
        private T[] _value = new T[5];
        private Type[] _key = new Type[5];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Insert(Type key, T value)
        {
            _count++;
            if (_count == _key.Length)
            {
                Array.Resize(ref _key, _count * 2);
                Array.Resize(ref _value, _count * 2);
            }
            _key[_count] = key;
            _value[_count] = value;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(Type key, out T value)
        {
            value = default;
            for (var i = _count - 1; i >= 0; i--)
            {
                if (_key[i] != key)
                    continue;
                value = _value[i];
                return true;
            }
            return false;
        }
    }
}