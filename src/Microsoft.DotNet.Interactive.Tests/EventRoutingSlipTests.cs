// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using System;
using System.Collections.Generic;


namespace Microsoft.DotNet.Interactive.Tests;

[TestClass]
public class EventRoutingSlipTests
{

    [TestMethod]
    public void can_be_stamped_with_kernel_uri()
    {
        var routingSlip = new EventRoutingSlip();
        routingSlip.Stamp(new Uri("kernel://1"));
        var other = new EventRoutingSlip();
        other.Stamp(new Uri("kernel://1"));
        routingSlip.StartsWith(other).Should().BeTrue();
    }


    [TestMethod]
    public void cannot_can_be_stamped_with_kernel_uri_twice()
    {
        var routingSlip = new EventRoutingSlip();
        routingSlip.Stamp(new Uri("kernel://1"));

        var markAgain = () => routingSlip.Stamp(new Uri("kernel://1"));

        markAgain.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("The uri kernel://1/ is already in the routing slip");
    }

    [TestMethod]
    public void can_continue_a_routingSlip_with_another()
    {
        var original = new EventRoutingSlip();
        original.Stamp(new Uri("kernel://1"));
        original.Stamp(new Uri("kernel://2"));


        var continuation = new EventRoutingSlip();
        continuation.Stamp(new Uri("kernel://3"));
        continuation.Stamp(new Uri("kernel://4"));

        original.ContinueWith(continuation);

        original.ToUriArray().Should().ContainInOrder(
            "kernel://1/",
            "kernel://2/",
            "kernel://3/",
            "kernel://4/");
    }

    [TestMethod]
    [DynamicData(nameof(EventRoutingSlipsToTest))]
    public void starts_with_urls(EventRoutingSlip other, bool startsWith)
    {
        var original = new EventRoutingSlip();
        original.Stamp(new Uri("kernel://1"));
        original.Stamp(new Uri("kernel://2"));
        original.Stamp(new Uri("kernel://3"));

       
        original.StartsWith(other).Should().Be(startsWith);
    }

    public static IEnumerable<object[]> EventRoutingSlipsToTest()
    {
        var routingSlip = new EventRoutingSlip();
        
        yield return new object[] { routingSlip, false};

        routingSlip = new EventRoutingSlip();
        routingSlip.Stamp(new Uri("kernel://1"));
        
        yield return new object[] { routingSlip, true};

        routingSlip = new EventRoutingSlip();
        routingSlip.Stamp(new Uri("kernel://1"));
        routingSlip.Stamp(new Uri("kernel://2"));
        
        yield return new object[] {routingSlip, true };

        routingSlip = new EventRoutingSlip();
        routingSlip.Stamp(new Uri("kernel://1"));
        routingSlip.Stamp(new Uri("kernel://2"));
        routingSlip.Stamp(new Uri("kernel://3"));
        
        yield return new object[] {  routingSlip, true };

        routingSlip = new EventRoutingSlip();
        routingSlip.Stamp(new Uri("kernel://1"));
        routingSlip.Stamp(new Uri("kernel://2"));
        routingSlip.Stamp(new Uri("kernel://4"));
        
        yield return new object[] { routingSlip, false };
    }

    [TestMethod]
    public void can_continue_a_routingSlip_to_another_skipping_entries_if_the_other_contains_it()
    {
        var original = new EventRoutingSlip();
        original.Stamp(new Uri("kernel://1"));
        original.Stamp(new Uri("kernel://2"));


        var continuation = new EventRoutingSlip();
        continuation.Stamp(new Uri("kernel://1"));
        continuation.Stamp(new Uri("kernel://2"));
        continuation.Stamp(new Uri("kernel://3"));
        continuation.Stamp(new Uri("kernel://4"));

        original.ContinueWith(continuation);

        original.ToUriArray().Should().ContainInOrder(
            "kernel://1/",
            "kernel://2/",
            "kernel://3/",
            "kernel://4/");
    }

    [TestMethod]
    public void throws_exception_when_continuing_a_routingSlip_with_another_if_they_do_not_start_with_same_uri_sequence()
    {
        var original = new EventRoutingSlip();
        original.Stamp(new Uri("kernel://1"));
        original.Stamp(new Uri("kernel://2"));
        original.Stamp(new Uri("kernel://3"));


        var continuation = new EventRoutingSlip();
        continuation.Stamp(new Uri("kernel://1"));
        continuation.Stamp(new Uri("kernel://3"));
        continuation.Stamp(new Uri("kernel://4"));

        var appendAction = () => original.ContinueWith(continuation);

        appendAction.Should().ThrowExactly<InvalidOperationException>().WithMessage("The uri kernel://1/ is already in the routing slip");
    }
}