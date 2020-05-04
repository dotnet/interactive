using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    /// <summary>
    /// Implements <see cref="IDictionary{,}" />, but not <see cref="IDictionary"/>.
    /// </summary>
    internal class GenericDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> _values = new Dictionary<TKey, TValue>();

        public TValue this[TKey key] { get => ((IDictionary<TKey, TValue>)_values)[key]; set => ((IDictionary<TKey, TValue>)_values)[key] = value; }

        public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)_values).Keys;

        public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)_values).Values;

        public int Count => ((IDictionary<TKey, TValue>)_values).Count;

        public bool IsReadOnly => ((IDictionary<TKey, TValue>)_values).IsReadOnly;

        public void Add(TKey key, TValue value)
        {
            ((IDictionary<TKey, TValue>)_values).Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((IDictionary<TKey, TValue>)_values).Add(item);
        }

        public void Clear()
        {
            ((IDictionary<TKey, TValue>)_values).Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)_values).Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return ((IDictionary<TKey, TValue>)_values).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>)_values).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IDictionary<TKey, TValue>)_values).GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            return ((IDictionary<TKey, TValue>)_values).Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)_values).Remove(item);
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            return ((IDictionary<TKey, TValue>)_values).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary<TKey, TValue>)_values).GetEnumerator();
        }
    }

    /// <summary>
    /// Implements <see cref="IDictionary" />, but not <see cref="IDictionary{,}"/>.
    /// </summary>
    internal class NonGenericDictionary : IDictionary
    {
        private Dictionary<object, object> _values = new Dictionary<object, object>();

        public object this[object key] { get => ((IDictionary)_values)[key]; set => ((IDictionary)_values)[key] = value; }

        public bool IsFixedSize => ((IDictionary)_values).IsFixedSize;

        public bool IsReadOnly => ((IDictionary)_values).IsReadOnly;

        public ICollection Keys => ((IDictionary)_values).Keys;

        public ICollection Values => ((IDictionary)_values).Values;

        public int Count => ((IDictionary)_values).Count;

        public bool IsSynchronized => ((IDictionary)_values).IsSynchronized;

        public object SyncRoot => ((IDictionary)_values).SyncRoot;

        public void Add(object key, object value)
        {
            ((IDictionary)_values).Add(key, value);
        }

        public void Clear()
        {
            ((IDictionary)_values).Clear();
        }

        public bool Contains(object key)
        {
            return ((IDictionary)_values).Contains(key);
        }

        public void CopyTo(Array array, int index)
        {
            ((IDictionary)_values).CopyTo(array, index);
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return ((IDictionary)_values).GetEnumerator();
        }

        public void Remove(object key)
        {
            ((IDictionary)_values).Remove(key);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary)_values).GetEnumerator();
        }
    }
}
