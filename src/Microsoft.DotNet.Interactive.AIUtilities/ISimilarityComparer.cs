// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



namespace Microsoft.DotNet.Interactive.AIUtilities;

public interface ISimilarityComparer<in T>
{
    public float Score(T a, T b);
}