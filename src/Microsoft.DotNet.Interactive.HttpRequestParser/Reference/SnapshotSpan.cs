// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.HttpRequest.Reference;

internal record SnapshotSpan(int Start, int Length)
{
    public int End => Start + Length - 1;

    public bool IntersectsWith(SnapshotSpan span) =>
        span.Start <= End && span.End >= Start;
}
