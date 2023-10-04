// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable once CheckNamespace

namespace System.Collections;

public interface ISimilarityComparer<in T, in TQuery>
{
    public float Score(T a, T b);
    public float Score(T a, TQuery query);
}