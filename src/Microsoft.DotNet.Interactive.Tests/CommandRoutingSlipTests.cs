// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;
internal static class CommandRoutingSlipExtensions
{
    public static void FullStamp(this CommandRoutingSlip routingSlip, Uri kernelUri)
    {
        routingSlip.StampAsArrived(kernelUri);
        routingSlip.Stamp(kernelUri);
    }
}

public class CommandRoutingSlipTests
{
    [Fact]
    public void can_be_stamped_with_kernel_uri_on_arrival()
    {
        var routingSlip = new CommandRoutingSlip();
        routingSlip.StampAsArrived(new Uri("kernel://pid-1234"));
        routingSlip.StartsWith(new Uri("kernel://pid-1234")).Should().BeTrue();
    }

    [Fact]
    public void the_uri_array_contains_only_stamped_kernel_uri()
    {
        var routingSlip = new CommandRoutingSlip();
        routingSlip.StampAsArrived(new Uri("kernel://pid-1234"));
        routingSlip.Stamp(new Uri("kernel://pid-1234"));
        routingSlip.StampAsArrived(new Uri("kernel://pid-5678"));
        routingSlip.Stamp(new Uri("kernel://pid-5678"));
        routingSlip.StampAsArrived(new Uri("kernel://pid-91011"));

        routingSlip.ToUriArray().Should().ContainInOrder(
            new Uri("kernel://pid-1234"),
            new Uri("kernel://pid-5678"));
    }

    [Fact]
    public void can_be_stamped_with_kernel_uri()
    {
        var routingSlip = new CommandRoutingSlip();
        routingSlip.StampAsArrived(new Uri("kernel://pid-1234"));
        routingSlip.Stamp(new Uri("kernel://pid-1234"));
        routingSlip.StartsWith(new Uri("kernel://pid-1234")).Should().BeTrue();
    }

    [Fact]
    public void cannot_can_be_stamped_with_kernel_uri_on_arrival_twice()
    {
        var routingSlip = new CommandRoutingSlip();
        routingSlip.StampAsArrived(new Uri("kernel://pid-1234"));

        var markAgain = () => routingSlip.StampAsArrived(new Uri("kernel://pid-1234"));

        markAgain.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("The uri kernel://pid-1234/ is already in the routing slip");
    }

    [Fact]
    public void cannot_can_be_stamped_with_kernel_uri_if_the_uri_is_not_stamped_on_arrival()
    {
        var routingSlip = new CommandRoutingSlip();

        var markAgain = () => routingSlip.Stamp(new Uri("kernel://pid-1234"));

        markAgain.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("The uri kernel://pid-1234/ is not in the routing slip or has already been completed");
    }

    [Fact]
    public void can_append_a_routingSlip_to_another()
    {
        var original = new CommandRoutingSlip();
        original.FullStamp(new Uri("kernel://pid-1234"));
        original.FullStamp(new Uri("kernel://pid-5678"));


        var toBeAppended = new CommandRoutingSlip();
        toBeAppended.FullStamp(new Uri("kernel://pid-4321"));
        toBeAppended.FullStamp(new Uri("kernel://pid-8765"));

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
        var original = new CommandRoutingSlip();
        original.FullStamp(new Uri("kernel://pid-1234"));
        original.FullStamp(new Uri("kernel://pid-5678"));


        var toBeAppended = new CommandRoutingSlip();
        toBeAppended.FullStamp(new Uri("kernel://pid-1234"));
        toBeAppended.FullStamp(new Uri("kernel://pid-5678"));
        toBeAppended.FullStamp(new Uri("kernel://pid-4321"));
        toBeAppended.FullStamp(new Uri("kernel://pid-8765"));

        original.Append(toBeAppended);

        original.ToUriArray().Should().ContainInOrder(
            new Uri("kernel://pid-1234"),
            new Uri("kernel://pid-5678"),
            new Uri("kernel://pid-4321"),
            new Uri("kernel://pid-8765"));
    }

    [Fact]
    public void can_append_a_routingSlip_to_another_contains_only_fully_Stamped_kernel_uris()
    {
        var original = new CommandRoutingSlip();
        original.FullStamp(new Uri("kernel://pid-1234"));
        original.FullStamp(new Uri("kernel://pid-5678"));


        var toBeAppended = new CommandRoutingSlip();
        toBeAppended.FullStamp(new Uri("kernel://pid-1234"));
        toBeAppended.FullStamp(new Uri("kernel://pid-5678"));
        toBeAppended.FullStamp(new Uri("kernel://pid-4321"));
        toBeAppended.StampAsArrived(new Uri("kernel://pid-8765"));

        original.Append(toBeAppended);

        original.ToUriArray().Should().NotContain(
            new Uri("kernel://pid-8765"));
    }

    [Fact]
    public void fails_to_append_a_routingSlip_to_another_if_they_do_not_start_with_same_uris()
    {
        var original = new CommandRoutingSlip();
        original.FullStamp(new Uri("kernel://pid-1234"));
        original.FullStamp(new Uri("kernel://pid-5678"));
        original.FullStamp(new Uri("kernel://pid-4321"));


        var toBeAppended = new CommandRoutingSlip();
        toBeAppended.FullStamp(new Uri("kernel://pid-1234"));
        toBeAppended.FullStamp(new Uri("kernel://pid-4321"));
        toBeAppended.FullStamp(new Uri("kernel://pid-8765"));

        var appendAction = () => original.Append(toBeAppended);

        appendAction.Should().ThrowExactly<InvalidOperationException>().WithMessage("The uri kernel://pid-1234/ is already in the routing slip");
    }
    
}