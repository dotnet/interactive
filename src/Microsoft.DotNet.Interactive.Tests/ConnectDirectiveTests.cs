// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class ConnectDirectiveTests
    {
        [Fact]
        public void connect_command_is_not_available_by_default()
        {
            using var compositeKernel = new CompositeKernel();

            compositeKernel.Directives
                           .Should()
                           .NotContain(c => c.Name == "#!connect");
        }

        [Fact]
        public void connect_command_is_available_when_a_user_adds_a_kernel_connection_type()
        {
            using var compositeKernel = new CompositeKernel();

            compositeKernel.UseConnection(new ConnectNamedPipe());

            compositeKernel.Directives
                           .Should()
                           .Contain(c => c.Name == "#!connect");
        }

        [Fact]
        public async Task When_a_kernel_is_connected_then_information_about_it_is_displayed()
        {
            using var kernel = CreateKernelWithConnectableFakeKernel();

            var result = await kernel.SubmitCodeAsync("#!connect --kernel-name my-fake-kernel fake --fakeness-level 9000");

            result.KernelEvents
                  .ToSubscribedList()
                  .Should()
                  .ContainSingle<DisplayedValueProduced>()
                  .Which
                  .FormattedValues
                  .Should()
                  .ContainSingle()
                  .Which
                  .Value
                  .Should()
                  .Be("Kernel added: #!my-fake-kernel");
        }

        [Fact]
        public async Task When_a_new_kernel_is_connected_then_it_becomes_addressable_by_name()
        {
            var wasCalled = false;
            var fakeKernel = new FakeKernel
            {
                Handle = (command, context) =>
                {
                    wasCalled = true;
                    return Task.CompletedTask;
                }
            };

            using var kernel = CreateKernelWithConnectableFakeKernel(fakeKernel);

            await kernel.SubmitCodeAsync("#!connect --kernel-name my-fake-kernel fake --fakeness-level 9000");

            await kernel.SubmitCodeAsync(@"
#!my-fake-kernel
hello!
");
            wasCalled.Should().BeTrue();

        }

        [Fact]
        public async Task Connected_kernels_are_disposed_when_composite_kernel_is_disposed()
        {
            var disposed = false;

            var fakeKernel = new FakeKernel();
            fakeKernel.RegisterForDisposal(() => disposed = true);

            var compositeKernel = CreateKernelWithConnectableFakeKernel(fakeKernel);
            
            await compositeKernel.SubmitCodeAsync("#!connect --kernel-name my-fake-kernel fake --fakeness-level 9000");
            compositeKernel.Dispose();

            disposed.Should().BeTrue();
        }

        private static Kernel CreateKernelWithConnectableFakeKernel(FakeKernel fakeKernel = null)
        {
            using var compositeKernel = new CompositeKernel
            {
                new FakeKernel("x")
            };

            compositeKernel.UseConnection(
                new ConnectFakeKernel("fake", "Connects the fake kernel")
                {
                    CreateKernel = (options, context) => Task.FromResult<Kernel>(fakeKernel ?? new FakeKernel())
                });

            return compositeKernel;
        }

        public class ConnectFakeKernel : ConnectKernelCommand<FakeKernelConnectionOptions>
        {
            public ConnectFakeKernel(string name, string description) : base(name, description)
            {
                AddOption(new Option<int>("--fakeness-level"));
            }

            public Func<FakeKernelConnectionOptions, KernelInvocationContext, Task<Kernel>> CreateKernel { get; set; }

            public override Task<Kernel> CreateKernelAsync(FakeKernelConnectionOptions options, KernelInvocationContext context)
            {
                return CreateKernel(options, context);
            }
        }
    }

    public class FakeKernelConnectionOptions : KernelConnectionOptions
    {


        public int FakenessLevel { get; set; }
    }
}