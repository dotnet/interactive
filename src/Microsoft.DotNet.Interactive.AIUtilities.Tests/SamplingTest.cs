// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.AIUtilities.Tests;

public class SamplingTest
{
    [Fact]
    public void can_shuffle_collection()
    {
        var src = Enumerable.Range(0, 10);
        var shuffled = src.Shuffle().ToArray();
        shuffled.Should().NotBeInAscendingOrder();
    }

    [Fact]
    public void does_not_duplicate_items()
    {
        var src = Enumerable.Range(0, 10).ToArray();
        var shuffled = src.Shuffle().Distinct().ToArray();
        shuffled.Should().HaveCount(src.Length);
    }
}