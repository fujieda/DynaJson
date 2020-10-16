using System;
using System.Runtime.CompilerServices;

namespace DynaJson
{
    internal class Stack<T>
    {
        private T[] _array = new T[8];

        public int Count { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(T obj)
        {
            if (Count == _array.Length)
                Array.Resize(ref _array, _array.Length * 2);
            _array[Count++] = obj;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            return _array[--Count];
        }
    }
}