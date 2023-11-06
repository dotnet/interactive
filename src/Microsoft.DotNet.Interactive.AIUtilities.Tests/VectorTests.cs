// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.AIUtilities.Tests;

public class VectorTests
{
    [Fact]
    public void can_calculate_average_of_vectors()
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

    [Fact]
    public void can_calculate_weighted_centroid_of_vectors()
    {
        var data = new[]
        {
            new []{ 1f, 1f, 1f},
            new []{ 2f, 2f, 2f},
            new []{ 1f, 2f, 3f}
        };

        var centroid = data.Centroid(d => d.Last());

        centroid.Should().BeEquivalentTo(new[] { 4.3333335F, 4.6666665F, 5F });
    }

    [Fact]
    public void can_calculate_average_of_vector_stream()
    {
        var data = new[]
        {
            new []{ 1f, 1f, 1f},
            new []{ 2f, 2f, 2f},
            new []{ 1f, 2f, 3f}
        };

        var stream = new ReplaySubject<float[]>();

        var centroids = new List<float[]>();

        using var _=  stream.Centroid(3).Subscribe( centroids.Add);

        foreach (var vector in data)
        {
            stream.OnNext(vector);
        }

        centroids.Should().BeEquivalentTo(new [] {
            new []{ 1f, 1f, 1f},
            new []{ 1.5f, 1.5f, 1.5f},
            new[] { 1.3333334F, 1.6666666F, 2F }
            });
    }
}