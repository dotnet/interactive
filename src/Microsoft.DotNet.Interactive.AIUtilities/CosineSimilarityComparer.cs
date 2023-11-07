// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Pocket;

namespace Microsoft.DotNet.Interactive.AIUtilities;

public class CosineSimilarityComparer<T> : ISimilarityComparer<T>
{
    private readonly Func<T, float[]> _toVector;

    public CosineSimilarityComparer(Func<T, float[]> toFloatVector)
    {
        Logger.Log.Event();
        _toVector = toFloatVector ?? throw new ArgumentNullException(nameof(toFloatVector));
    }

    public float Score(T a, T b)
    {
        Logger.Log.Event();
        var va = _toVector(a);
        var vb = _toVector(b);
        return CosineSimilarity(va, vb);
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