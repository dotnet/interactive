// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Tests;

[TestClass]
public class KernelExtensionsTests
{
    [TestMethod]
    public void FindKernelByName_finds_a_subkernel_of_a_composite_kernel_by_name()
    {
        var one = new FakeKernel("one");
        var two = new FakeKernel("two");
        using var compositeKernel = new CompositeKernel
        {
            one,
            two,
        };

        var found = compositeKernel.FindKernelByName("two");

        found.Should().BeSameAs(two);
    }

    [TestMethod]
    public void FindKernelByName_finds_a_subkernel_of_a_composite_kernel_by_alias()
    {
        var one = new FakeKernel("one");
        var two = new FakeKernel("two");
        using var compositeKernel = new CompositeKernel();
        compositeKernel.Add(one, aliases: new[] { "one-alias" });
        compositeKernel.Add(two);

        var found = compositeKernel.FindKernelByName("one-alias");

        found.Should().BeSameAs(one);
    }

    [TestMethod]
    public void FindKernelByName_finds_a_subkernel_of_a_parent_composite_kernel_by_name()
    {
        var one = new FakeKernel("one");
        var two = new FakeKernel("two");
        using var compositeKernel = new CompositeKernel
        {
            one,
            two,
        };

        var found = one.FindKernelByName("two");

        found.Should().BeSameAs(two);
    }

    [TestMethod]
    public void FindKernelByName_returns_null_for_unknown_kernel()
    {
        var one = new FakeKernel("one");
        var two = new FakeKernel("two");
        using var compositeKernel = new CompositeKernel
        {
            one,
            two,
        };

        var found = compositeKernel.FindKernelByName("three");

        found.Should().BeNull();
    }
}