// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.AIUtilities;

public static class ScoredValue
{
    public static ScoredValue<T> Create<T>(T value, float score) => new(value, score);
}

public class ScoredValue<T>
{
    public ScoredValue(T value, float score)
    {
        Value = value;
        Score = score;
    }

    public T Value { get; }
    public float Score { get; }

}