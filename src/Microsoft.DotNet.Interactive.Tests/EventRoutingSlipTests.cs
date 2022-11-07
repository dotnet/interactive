// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using System;

using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public class EventRoutingSlipTests
{

    [Fact]
    public void can_be_stamped_with_kernel_uri()
    {
        var routingSlip = new EventRoutingSlip();
        routingSlip.Stamp(new Uri("kernel://pid-1234"));
        routingSlip.StartsWith(new Uri("kernel://pid-1234")).Should().BeTrue();
    }


    [Fact]
    public void cannot_can_be_stamped_with_kernel_uri_twice()
    {
        var routingSlip = new EventRoutingSlip();
        routingSlip.Stamp(new Uri("kernel://pid-1234"));

        var markAgain = () => routingSlip.Stamp(new Uri("kernel://pid-1234"));

        markAgain.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("The uri kernel://pid-1234/ is already in the routing slip");
    }

    [Fact]
    public void can_append_a_routingSlip_to_another()
    {
        var original = new EventRoutingSlip();
        original.Stamp(new Uri("kernel://pid-1234"));
        original.Stamp(new Uri("kernel://pid-5678"));


        var toBeAppended = new EventRoutingSlip();
        toBeAppended.Stamp(new Uri("kernel://pid-4321"));
        toBeAppended.Stamp(new Uri("kernel://pid-8765"));

        original.Append(toBeAppended);

        original.ToUriArray().Should().ContainInOrder(
            new Uri("kernel://pid-1234"),
            new Uri("kernel://pid-5678"),
            new Uri("kernel://pid-4321"),
            new Uri("kernel://pid-8765"));
    }

    [Fact]
    public void can_append_a_routingSlip_to_another_skipping_entries_if_the_other_contains_it()
    {
        var original = new EventRoutingSlip();
        original.Stamp(new Uri("kernel://pid-1234"));
        original.Stamp(new Uri("kernel://pid-5678"));


        var toBeAppended = new EventRoutingSlip();
        toBeAppended.Stamp(new Uri("kernel://pid-1234"));
        toBeAppended.Stamp(new Uri("kernel://pid-5678"));
        toBeAppended.Stamp(new Uri("kernel://pid-4321"));
        toBeAppended.Stamp(new Uri("kernel://pid-8765"));

        original.Append(toBeAppended);

        original.ToUriArray().Should().ContainInOrder(
            new Uri("kernel://pid-1234"),
            new Uri("kernel://pid-5678"),
            new Uri("kernel://pid-4321"),
            new Uri("kernel://pid-8765"));
    }

    [Fact]
    public void fails_to_append_a_routingSlip_to_another_if_they_do_not_start_with_same_uris()
    {
        var original = new EventRoutingSlip();
        original.Stamp(new Uri("kernel://pid-1234"));
        original.Stamp(new Uri("kernel://pid-5678"));
        original.Stamp(new Uri("kernel://pid-4321"));


        var toBeAppended = new EventRoutingSlip();
        toBeAppended.Stamp(new Uri("kernel://pid-1234"));
        toBeAppended.Stamp(new Uri("kernel://pid-4321"));
        toBeAppended.Stamp(new Uri("kernel://pid-8765"));

        var appendAction = () => original.Append(toBeAppended);

        appendAction.Should().ThrowExactly<InvalidOperationException>().WithMessage("The uri kernel://pid-1234/ is already in the routing slip");
    }
}