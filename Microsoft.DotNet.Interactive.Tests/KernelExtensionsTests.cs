// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

        [Fact]
        public void VisitSubkernels_does_not_recurse_by_default()
        {
            var visited = new List<string>();
            var child = new CompositeKernel
            {
                Name = "child"
            };
            var grandchild = new CompositeKernel
            {
                Name = "grandchild"
            };
            var parent = new CompositeKernel
            {
                Name = "parent"
            };
            child.Add(grandchild);
            parent.Add(child);

            parent.VisitSubkernels(kernel => visited.Add(kernel.Name));

            visited.Should().BeEquivalentTo("child");
        }

        [Fact]
        public void VisitSubkernels_can_recurse_child_composite_kernels()
        {
            var visited = new List<string>();
            var child = new CompositeKernel
            {
                Name = "child"
            };
            var grandchild = new CompositeKernel
            {
                Name = "grandchild"
            };
            var parent = new CompositeKernel
            {
                Name = "parent"
            };
            child.Add(grandchild);
            parent.Add(child);

            parent.VisitSubkernels(kernel => visited.Add(kernel.Name), true);

            visited.Should().BeEquivalentTo("child", "grandchild");
        }

        [Fact]
        public async Task VisitSubkernelsAsync_does_not_recurse_by_default()
        {
            var visited = new List<string>();
            var child = new CompositeKernel
            {
                Name = "child"
            };
            var grandchild = new CompositeKernel
            {
                Name = "grandchild"
            };
            var parent = new CompositeKernel
            {
                Name = "parent"
            };
            child.Add(grandchild);
            parent.Add(child);

            await parent.VisitSubkernelsAsync(kernel =>
            {
                visited.Add(kernel.Name);
                return Task.CompletedTask;
            });

            visited.Should().BeEquivalentTo("child");
        }

        [Fact]
        public async Task VisitSubkernelsAsync_can_recurse_child_composite_kernels()
        {
            var visited = new List<string>();
            var child = new CompositeKernel
            {
                Name = "child"
            };
            var grandchild = new CompositeKernel
            {
                Name = "grandchild"
            };
            var parent = new CompositeKernel
            {
                Name = "parent"
            };
            child.Add(grandchild);
            parent.Add(child);

            await parent.VisitSubkernelsAsync(kernel =>
            {
                visited.Add(kernel.Name);
                return Task.CompletedTask;
            }, true);

            visited.Should().BeEquivalentTo("child", "grandchild");
        }
    }
}