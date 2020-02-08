// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class KernelExtensionsTests
    {
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