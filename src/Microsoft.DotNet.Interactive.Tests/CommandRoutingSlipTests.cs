// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;

using FluentAssertions;

namespace Microsoft.DotNet.Interactive.Tests;
internal static class CommandRoutingSlipExtensions
{
    public static void FullStamp(this CommandRoutingSlip routingSlip, Uri kernelUri)
    {
        routingSlip.StampAsArrived(kernelUri);
        routingSlip.Stamp(kernelUri);
    }
}

[TestClass]
public class CommandRoutingSlipTests
{
    [TestMethod]
    public void can_be_stamped_with_kernel_uri_on_arrival()
    {
        var routingSlip = new CommandRoutingSlip();
        routingSlip.StampAsArrived(new Uri("kernel://1"));
        var other = new CommandRoutingSlip();
        other.StampAsArrived(new Uri("kernel://1"));
        routingSlip.StartsWith(other).Should().BeTrue();
    }

    [TestMethod]
    public void the_uri_array_contains_all_uris()
    {
        var routingSlip = new CommandRoutingSlip();
        routingSlip.StampAsArrived(new Uri("kernel://1"));
        routingSlip.Stamp(new Uri("kernel://1"));
        routingSlip.StampAsArrived(new Uri("kernel://2"));
        routingSlip.Stamp(new Uri("kernel://2"));
        routingSlip.StampAsArrived(new Uri("kernel://3"));

        routingSlip.ToUriArray().Should().ContainInOrder(
            "kernel://1/?tag=arrived",
            "kernel://1/",
            "kernel://2/?tag=arrived",
            "kernel://2/",
            "kernel://3/?tag=arrived");
    }

    [TestMethod]
    public void can_be_stamped_with_kernel_uri()
    {
        var routingSlip = new CommandRoutingSlip();
        routingSlip.StampAsArrived(new Uri("kernel://1"));
        routingSlip.Stamp(new Uri("kernel://1"));

        var other = new CommandRoutingSlip();
        other.StampAsArrived(new Uri("kernel://1"));
        routingSlip.StartsWith(other).Should().BeTrue();
    }

    [TestMethod]
    public void cannot_can_be_stamped_with_kernel_uri_on_arrival_twice()
    {
        var routingSlip = new CommandRoutingSlip();
        routingSlip.StampAsArrived(new Uri("kernel://1"));

        var markAgain = () => routingSlip.StampAsArrived(new Uri("kernel://1"));

        markAgain.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("The uri kernel://1/?tag=arrived is already in the routing slip");
    }

    [TestMethod]
    public void cannot_can_be_stamped_with_kernel_uri_if_the_uri_is_not_stamped_on_arrival()
    {
        var routingSlip = new CommandRoutingSlip();

        var markAgain = () => routingSlip.Stamp(new Uri("kernel://1"));

        markAgain.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("The uri kernel://1/ is not in the routing slip");
    }

    [TestMethod]
    public void can_continue_a_routingSlip_with_another()
    {
        var original = new CommandRoutingSlip();
        original.FullStamp(new Uri("kernel://1"));
        original.FullStamp(new Uri("kernel://2"));


        var continuation = new CommandRoutingSlip();
        continuation.FullStamp(new Uri("kernel://3"));
        continuation.FullStamp(new Uri("kernel://4"));

        original.ContinueWith(continuation);

        original.ToUriArray().Should().ContainInOrder(
            "kernel://1/",
            "kernel://2/",
            "kernel://3/",
            "kernel://4/");
    }

    [TestMethod]
    public void can_continue_a_routingSlip_to_another_skipping_entries_if_the_other_contains_it()
    {
        var original = new CommandRoutingSlip();
        original.FullStamp(new Uri("kernel://1"));
        original.FullStamp(new Uri("kernel://2"));


        var continuation = new CommandRoutingSlip();
        continuation.FullStamp(new Uri("kernel://1"));
        continuation.FullStamp(new Uri("kernel://2"));
        continuation.FullStamp(new Uri("kernel://3"));
        continuation.FullStamp(new Uri("kernel://4"));

        original.ContinueWith(continuation);

        original.ToUriArray().Should().ContainInOrder(
            "kernel://1/",
            "kernel://2/",
            "kernel://3/",
            "kernel://4/");
    }

    [TestMethod]
    [DynamicData(nameof(CommandRoutingSlipsToTest))]
    public void starts_with_urls(CommandRoutingSlip other, bool startsWith)
    {
        var original = new CommandRoutingSlip();
        original.FullStamp(new Uri("kernel://1"));
        original.FullStamp(new Uri("kernel://2"));
        original.FullStamp(new Uri("kernel://3"));

        original.StartsWith(other).Should().Be(startsWith);
    }

    public static IEnumerable<object[]> CommandRoutingSlipsToTest()
    {
        var routingSlip = new CommandRoutingSlip();
        
        yield return new object[] { routingSlip, false };

        routingSlip = new CommandRoutingSlip();
        routingSlip.FullStamp(new Uri("kernel://1"));
        
        yield return new object[] { routingSlip, true };

        routingSlip = new CommandRoutingSlip();
        routingSlip.FullStamp(new Uri("kernel://1"));
        routingSlip.FullStamp(new Uri("kernel://2"));

        yield return new object[] { routingSlip, true };

        routingSlip = new CommandRoutingSlip();
        routingSlip.FullStamp(new Uri("kernel://1"));
        routingSlip.FullStamp(new Uri("kernel://2"));
        routingSlip.FullStamp(new Uri("kernel://3"));

        yield return new object[] { routingSlip, true };

        routingSlip = new CommandRoutingSlip();
        routingSlip.FullStamp(new Uri("kernel://1"));
        routingSlip.FullStamp(new Uri("kernel://2"));
        routingSlip.FullStamp(new Uri("kernel://3"));
        routingSlip.StampAsArrived(new Uri("kernel://4"));
        yield return new object[] {routingSlip, false };
    }

    [TestMethod]
    public void continuing_a_routingSlip_with_another_contains_all_kernel_uris()
    {
        var original = new CommandRoutingSlip();
        original.FullStamp(new Uri("kernel://1"));
        original.FullStamp(new Uri("kernel://2"));


        var continuation = new CommandRoutingSlip();
        continuation.FullStamp(new Uri("kernel://1"));
        continuation.FullStamp(new Uri("kernel://2"));
        continuation.FullStamp(new Uri("kernel://3"));
        continuation.StampAsArrived(new Uri("kernel://4"));

        original.ContinueWith(continuation);

        original.ToUriArray().Should().Contain(
            "kernel://4/?tag=arrived");
    }

    [TestMethod]
    public void throws_exception_when_continuing_a_routingSlip_with_another_if_they_do_not_start_with_same_uri_sequence()
    {
        var original = new CommandRoutingSlip();
        original.FullStamp(new Uri("kernel://1"));
        original.FullStamp(new Uri("kernel://2"));
        original.FullStamp(new Uri("kernel://3"));


        var continuation = new CommandRoutingSlip();
        continuation.FullStamp(new Uri("kernel://1"));
        continuation.FullStamp(new Uri("kernel://3"));
        continuation.FullStamp(new Uri("kernel://4"));

        var appendAction = () => original.ContinueWith(continuation);

        appendAction.Should().ThrowExactly<InvalidOperationException>().WithMessage("The uri kernel://1/ is already in the routing slip");
    }
    
}