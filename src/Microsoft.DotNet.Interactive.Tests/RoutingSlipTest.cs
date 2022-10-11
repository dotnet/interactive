// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public class RoutingSlipTest
{
    [Fact]
    public void can_marked_when_received()
    {
        var routingSlip = new RoutingSlip();
        routingSlip.MarkAsReceived(new Uri("kernel://pid-1234"));
        
        throw new NotImplementedException();
    }

    [Fact]
    public void can_be_marked_when_completed()
    {
        var routingSlip = new RoutingSlip();
        routingSlip.MarkAsCompleted(new Uri("kernel://pid-1234"));
        throw new NotImplementedException();
    }

    [Fact]
    public void when_marked_as_completed_cannot_be_marked_again_as_received()
    {
        var routingSlip = new RoutingSlip();
        routingSlip.MarkAsCompleted(new Uri("kernel://pid-1234"));

        var markAgain = () => routingSlip.MarkAsReceived(new Uri("kernel://pid-1234"));

        markAgain.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void when_marked_as_completed_cannot_be_marked_again_as_completed()
    {
        var routingSlip = new RoutingSlip();
        routingSlip.MarkAsCompleted(new Uri("kernel://pid-1234"));

        var markAgain = () => routingSlip.MarkAsCompleted(new Uri("kernel://pid-1234"));

        markAgain.Should().Throw<InvalidOperationException>();
    }
}