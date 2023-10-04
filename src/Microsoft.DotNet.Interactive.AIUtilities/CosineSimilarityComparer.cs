// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable once CheckNamespace
namespace System.Collections;

public class CosineSimilarityComparer<T> : ISimilarityComparer<T, float[]>
{
    private readonly Func<T, float[]> _toVector;

    public CosineSimilarityComparer(Func<T, float[]> toVector)
    {
        _toVector = toVector ?? throw new ArgumentNullException(nameof(toVector));
    }
    public float Score(T a, T b)
    {
        var vb = _toVector(b);
        return Score(a, vb);
    }

    public float Score(T a, float[] query)
    {
        var va = _toVector(a);

        return CosineSimilarity(va, query);
    }

    private float CosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
            throw new ArgumentException("Vectors must have the same length");

        var dotProduct = 0f;
        var magnitude1 = 0f;
        var magnitude2 = 0f;

        for (var i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = MathF.Sqrt(magnitude1);
        magnitude2 = MathF.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
            return 0;  // handle the case where one or both vectors have zero magnitude

        return dotProduct / (magnitude1 * magnitude2);
    }
}