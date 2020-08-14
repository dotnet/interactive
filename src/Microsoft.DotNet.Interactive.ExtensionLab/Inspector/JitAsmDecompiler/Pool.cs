using System;
using System.Collections.Concurrent;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Inspector.JitAsmDecompiler
{
    public class Pool<T>
    {
        private readonly Func<T> _factory;
        private readonly ConcurrentBag<T> _pool = new ConcurrentBag<T>();

        public Pool(Func<T> factory) => _factory = factory;

        public Lease GetOrCreate()
        {
            if (!_pool.TryTake(out var value))
                value = _factory();

            return new Lease(this, value);
        }

        private void Return(T value) => _pool.Add(value);

        public readonly struct Lease : IDisposable
        {
            private readonly Pool<T> _pool;

            internal Lease(Pool<T> pool, T value)
            {
                _pool = pool;
                Object = value;
            }

            public T Object { get; }

            public void Dispose() => _pool.Return(Object);
        }
    }
}
