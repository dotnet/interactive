// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class KernelExtensionsTests
    {
        [Fact]
        public void FindKernel_finds_a_subkernel_of_a_composite_kernel_by_name()
        {
            var one = new FakeKernel("one");
            var two = new FakeKernel("two");
            using var compositeKernel = new CompositeKernel
            {
                one,
                two,
            };

            var found = compositeKernel.FindKernel("two");

            found.Should().BeSameAs(two);
        }

        [Fact]
        public void FindKernel_finds_a_subkernel_of_a_composite_kernel_by_alias()
        {
            var one = new FakeKernel("one");
            var two = new FakeKernel("two");
            using var compositeKernel = new CompositeKernel();
            compositeKernel.Add(one, aliases: new[] { "one-alias" });
            compositeKernel.Add(two);

            var found = compositeKernel.FindKernel("one-alias");

            found.Should().BeSameAs(one);
        }

        [Fact]
        public void FindKernel_finds_a_subkernel_of_a_parent_composite_kernel_by_name()
        {
            var one = new FakeKernel("one");
            var two = new FakeKernel("two");
            using var compositeKernel = new CompositeKernel
            {
                one,
                two,
            };

            var found = one.FindKernel("two");

            found.Should().BeSameAs(two);
        }

        [Fact]
        public void FindKernel_returns_null_for_unknown_kernel()
        {
            var one = new FakeKernel("one");
            var two = new FakeKernel("two");
            using var compositeKernel = new CompositeKernel
            {
                one,
                two,
            };

            var found = compositeKernel.FindKernel("three");

            found.Should().BeNull();
        }
    }
}