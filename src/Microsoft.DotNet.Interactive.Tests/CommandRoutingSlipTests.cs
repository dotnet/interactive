// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;

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
        routingSlip.StampAsArrived(new Uri("kernel://1"));
        routingSlip.StartsWith(new Uri("kernel://1")).Should().BeTrue();
    }

    [Fact]
    public void the_uri_array_contains_only_stamped_kernel_uri()
    {
        var routingSlip = new CommandRoutingSlip();
        routingSlip.StampAsArrived(new Uri("kernel://1"));
        routingSlip.Stamp(new Uri("kernel://1"));
        routingSlip.StampAsArrived(new Uri("kernel://2"));
        routingSlip.Stamp(new Uri("kernel://2"));
        routingSlip.StampAsArrived(new Uri("kernel://3"));

        routingSlip.ToUriArray().Should().ContainInOrder(
            new Uri("kernel://1"),
            new Uri("kernel://2"));
    }

    [Fact]
    public void can_be_stamped_with_kernel_uri()
    {
        var routingSlip = new CommandRoutingSlip();
        routingSlip.StampAsArrived(new Uri("kernel://1"));
        routingSlip.Stamp(new Uri("kernel://1"));
        routingSlip.StartsWith(new Uri("kernel://1")).Should().BeTrue();
    }

    [Fact]
    public void cannot_can_be_stamped_with_kernel_uri_on_arrival_twice()
    {
        var routingSlip = new CommandRoutingSlip();
        routingSlip.StampAsArrived(new Uri("kernel://1"));

        var markAgain = () => routingSlip.StampAsArrived(new Uri("kernel://1"));

        markAgain.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("The uri kernel://1/ is already in the routing slip");
    }

    [Fact]
    public void cannot_can_be_stamped_with_kernel_uri_if_the_uri_is_not_stamped_on_arrival()
    {
        var routingSlip = new CommandRoutingSlip();

        var markAgain = () => routingSlip.Stamp(new Uri("kernel://1"));

        markAgain.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("The uri kernel://1/ is not in the routing slip or has already been completed");
    }

    [Fact]
    public void can_append_a_routingSlip_to_another()
    {
        var original = new CommandRoutingSlip();
        original.FullStamp(new Uri("kernel://1"));
        original.FullStamp(new Uri("kernel://2"));


        var toBeAppended = new CommandRoutingSlip();
        toBeAppended.FullStamp(new Uri("kernel://3"));
        toBeAppended.FullStamp(new Uri("kernel://4"));

        original.Append(toBeAppended);

        original.ToUriArray().Should().ContainInOrder(
            new Uri("kernel://1"),
            new Uri("kernel://2"),
            new Uri("kernel://3"),
            new Uri("kernel://4"));
    }

    [Fact]
    public void can_append_a_routingSlip_to_another_skipping_entries_if_the_other_contains_it()
    {
        var original = new CommandRoutingSlip();
        original.FullStamp(new Uri("kernel://1"));
        original.FullStamp(new Uri("kernel://2"));


        var toBeAppended = new CommandRoutingSlip();
        toBeAppended.FullStamp(new Uri("kernel://1"));
        toBeAppended.FullStamp(new Uri("kernel://2"));
        toBeAppended.FullStamp(new Uri("kernel://3"));
        toBeAppended.FullStamp(new Uri("kernel://4"));

        original.Append(toBeAppended);

        original.ToUriArray().Should().ContainInOrder(
            new Uri("kernel://1"),
            new Uri("kernel://2"),
            new Uri("kernel://3"),
            new Uri("kernel://4"));
    }

    [Theory]
    [MemberData(nameof(EventRoutingSlipsToTest))]
    public void starts_with_urls(Uri[] other, bool startsWith)
    {
        var original = new CommandRoutingSlip();
        original.FullStamp(new Uri("kernel://1"));
        original.FullStamp(new Uri("kernel://2"));
        original.FullStamp(new Uri("kernel://3"));

        original.StartsWith(other).Should().Be(startsWith);
    }

    public static IEnumerable<object[]> EventRoutingSlipsToTest()
    {
        yield return new object[] { Array.Empty<Uri>(), false };
        yield return new object[] { new[] { new Uri("kernel://1") }, true };
        yield return new object[] { new[] { new Uri("kernel://1"), new Uri("kernel://2") }, true };
        yield return new object[] { new[] { new Uri("kernel://1"), new Uri("kernel://2"), new Uri("kernel://3") }, true };
        yield return new object[] { new[] { new Uri("kernel://1"), new Uri("kernel://2"), new Uri("kernel://4") }, false };
    }

    [Fact]
    public void can_append_a_routingSlip_to_another_contains_only_fully_Stamped_kernel_uris()
    {
        var original = new CommandRoutingSlip();
        original.FullStamp(new Uri("kernel://1"));
        original.FullStamp(new Uri("kernel://2"));


        var toBeAppended = new CommandRoutingSlip();
        toBeAppended.FullStamp(new Uri("kernel://1"));
        toBeAppended.FullStamp(new Uri("kernel://2"));
        toBeAppended.FullStamp(new Uri("kernel://3"));
        toBeAppended.StampAsArrived(new Uri("kernel://4"));

        original.Append(toBeAppended);

        original.ToUriArray().Should().NotContain(
            new Uri("kernel://4"));
    }

    [Fact]
    public void fails_to_append_a_routingSlip_to_another_if_they_do_not_start_with_same_uris()
    {
        var original = new CommandRoutingSlip();
        original.FullStamp(new Uri("kernel://1"));
        original.FullStamp(new Uri("kernel://2"));
        original.FullStamp(new Uri("kernel://3"));


        var toBeAppended = new CommandRoutingSlip();
        toBeAppended.FullStamp(new Uri("kernel://1"));
        toBeAppended.FullStamp(new Uri("kernel://3"));
        toBeAppended.FullStamp(new Uri("kernel://4"));

        var appendAction = () => original.Append(toBeAppended);

        appendAction.Should().ThrowExactly<InvalidOperationException>().WithMessage("The uri kernel://1/ is already in the routing slip");
    }
    
}