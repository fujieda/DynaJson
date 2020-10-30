using System.Threading;

namespace DynaJson
{
    internal class BufferPool<T>
    {
        private class Node
        {
            public Node Next;
            public T Item;
        }

        private readonly Node _head = new Node();

        public void Return(T item)
        {
            var node = new Node {Item = item};
            do
            {
                node.Next = _head.Next;
            } while (Interlocked.CompareExchange(ref _head.Next, node, node.Next) != node.Next);
        }

        public T Rent()
        {
            Node node;
            do
            {
                node = _head.Next;
                if (node == null)
                    return default;
            } while (Interlocked.CompareExchange(ref _head.Next, node.Next, node) != node);
            return node.Item;
        }
    }
}