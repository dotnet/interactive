// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.AIUtilities.Tests;

public class VectorTests
{
    [Fact]
    public void can_calculate_centroid_of_vectors()
    {
        var data = new[]
        {
            new []{ 1f, 1f, 1f},
            new []{ 2f, 2f, 2f},
            new []{ 1f, 2f, 3f}
        };

        var centroid = data.Centroid();

        centroid.Should().BeEquivalentTo(new[]{ 1.3333334F, 1.6666666F, 2F });
    }
}