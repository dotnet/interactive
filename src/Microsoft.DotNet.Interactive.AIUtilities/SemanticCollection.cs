// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable once CheckNamespace
namespace System.Collections;

public class SemanticCollection<T, TQuery> : ICollection<T>
{
    private readonly List<T> _storage = new();
    private readonly ISimilarityComparer<T, TQuery> _similarityComparer;

    public SemanticCollection(ISimilarityComparer<T, TQuery> similarityComparer)
    {
        _similarityComparer = similarityComparer;
    }

    public SemanticCollection(ISimilarityComparer<T, TQuery> similarityComparer, IEnumerable<T> other)
    {
        _similarityComparer = similarityComparer;
        _storage.AddRange(other);
    }

    public IEnumerable<ScoredItem<T>> Search(TQuery query, int limit = 1, float threshold = 0.7f)
    {
        return  _storage.Select(item => new ScoredItem<T>( item, _similarityComparer.Score(item, query)))
            .OrderByDescending(entry => entry.Score).Where(entry => entry.Score >= threshold).Take(limit);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _storage.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_storage).GetEnumerator();
    }

    public void Add(T item)
    {
        _storage.Add(item);
    }

    public void Clear()
    {
        _storage.Clear();
    }

    public bool Contains(T item)
    {
        return _storage.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _storage.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        return _storage.Remove(item);
    }

    public int Count => _storage.Count;

    public bool IsReadOnly => false;
}

public static class EnumerableSearchExtensions{
    public static IEnumerable<ScoredItem<T>> Search<T, TQuery>(this IEnumerable<T> source, TQuery query,
        ISimilarityComparer<T, TQuery> comparer, int limit =1, float threshold = 0.8f)
    {
        return source.Select(item => new ScoredItem<T>(item, comparer.Score(item, query)))
            .OrderByDescending(entry => entry.Score).Where(entry => entry.Score >= threshold).Take(limit);
    }

    public static IEnumerable<ScoredItem<T>> Search<T, TQuery>(this IEnumerable<T> source, T query,
        ISimilarityComparer<T, TQuery> comparer, Func<T,TQuery> toQuery, int limit = 1, float threshold = 0.8f)
    {
        return source.Search(toQuery(query), comparer, limit, threshold);
    }
}