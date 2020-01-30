// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Pocket;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class CompositeKernelTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public CompositeKernelTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        [Fact(Timeout = 45000)]
        public async Task Handling_kernel_can_be_specified_using_kernel_name_as_a_magic_command()
        {
            // FIX: (Handling_kernel_can_be_specified_using_kernel_name_as_a_magic_command) 
            var cSharpKernel = new CSharpKernel();
            var fSharpKernel = new FSharpKernel();
            using var kernel = new CompositeKernel
            {
                cSharpKernel,
                fSharpKernel,
            };
            kernel.DefaultKernelName = fSharpKernel.Name;

            using var events = kernel.KernelEvents.ToSubscribedList();

            var csharpCommand = new SubmitCode(@"
#!csharp
new [] {1,2,3}");
            await kernel.SendAsync(csharpCommand);
            
//             var fsharpCommand = new SubmitCode(@"
// #!fsharp
// [1;2;3]");
//
//             await kernel.SendAsync(fsharpCommand);

            Pocket.Logger.Log.Info("" , events );

            events.Should()
                  .ContainSingle<CommandHandled>(e => e.Command == csharpCommand);
            // events.Should()
            //       .ContainSingle<CommandHandled>(e => e.Command == fsharpCommand);
        }

        [Theory(Timeout = 45000)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task when_target_kernel_is_specified_and_not_found_then_command_fails(int kernelCount)
        {
            using var kernel = new CompositeKernel();
            using var events = kernel.KernelEvents.ToSubscribedList();
            foreach (var kernelName in Enumerable.Range(0, kernelCount).Select(i => $"kernel{i}"))
            {
                    kernel.Add(new FakeKernel(kernelName));
            }

            await kernel.SendAsync(
                new SubmitCode(
                    @"var x = 123;",
                    "unregistered kernel name"));

            events.Should()
                .ContainSingle<CommandFailed>(cf => cf.Exception is NoSuitableKernelException);
        }

        [Fact(Timeout = 45000)]
        public void cannot_add_duplicated_named_kernels()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel()
            };

            kernel.Invoking(k => k.Add(new CSharpKernel()))
                .Should()
                .Throw<ArgumentException>()
                .Which
                .Message
                .Should()
                .Be("Kernel \"csharp\" already registered (Parameter 'kernel')");

        }

        [Fact(Timeout = 45000)]
        public async Task can_handle_commands_targeting_composite_kernel_directly()
        {
            using var kernel = new CompositeKernel
            {
                new FakeKernel("fake")
                {
                    Handle = (command, context) => Task.CompletedTask
                }
            };

            using var events = kernel.KernelEvents.ToSubscribedList();
            var submitCode = new SubmitCode("//command", kernel.Name)
            {
                Handler = (kernelCommand, context) => Task.CompletedTask
            };


            await kernel.SendAsync(submitCode);
            events.Should()
                .ContainSingle<CommandHandled>()
                .Which
                .Command
                .Should()
                .Be(submitCode);
        }

        [Fact(Timeout = 45000)]
        public async Task commands_targeting_compositeKernel_are_not_routed_to_childKernels()
        {
            var receivedOnFakeKernel = new List<IKernelCommand>();
            using var kernel = new CompositeKernel
            {
                new FakeKernel("fake")
                {
                    Handle = (kernelCommand, context) =>
                    {
                        receivedOnFakeKernel.Add(kernelCommand);
                        return Task.CompletedTask;
                    }
                }
            };

            var submitCode = new SubmitCode("//command", kernel.Name);
            await kernel.SendAsync(submitCode);
            receivedOnFakeKernel.Should()
                .BeEmpty();
        }

        [Fact(Timeout = 45000)]
        public async Task Handling_kernel_can_be_specified_by_setting_the_kernel_name_in_the_command()
        {
            var receivedOnFakeKernel = new List<IKernelCommand>();

            using var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FakeKernel("fake")
                {
                    Handle = (kernelCommand, context) =>
                    {
                        receivedOnFakeKernel.Add(kernelCommand);
                        return Task.CompletedTask;
                    }
                }
            };

            await kernel.SendAsync(
                new SubmitCode(
                    @"var x = 123;",
                    "csharp"));
            await kernel.SendAsync(
                new SubmitCode(
                    @"hello!",
                    "fake"));
            await kernel.SendAsync(
                new SubmitCode(
                    @"x",
                    "csharp"));

            receivedOnFakeKernel
                .Should()
                .ContainSingle(c => c is SubmitCode)
                .Which
                .As<SubmitCode>()
                .Code
                .Should()
                .Be("hello!");
        }

        [Fact(Timeout = 45000)]
        public async Task Handling_kernel_can_be_specified_in_middleware()
        {
            var receivedOnFakeKernel = new List<IKernelCommand>();

            using var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FakeKernel("fake")
                {
                    Handle = (kernelCommand, context) =>
                    {
                        receivedOnFakeKernel.Add(kernelCommand);
                        return Task.CompletedTask;
                    }
                }
            };

            var childKernels = kernel.ChildKernels;

            kernel.AddMiddleware(async (kernelCommand, context, next) =>
            {
                context.HandlingKernel = childKernels.Single(k => k.Name == "fake");
                await next(kernelCommand, context);
            });

            await kernel.SendAsync(new SubmitCode("hello!"));

            receivedOnFakeKernel
                .Should()
                .ContainSingle(c => c is SubmitCode)
                .Which
                .As<SubmitCode>()
                .Code
                .Should()
                .Be("hello!");
        }

        [Fact(Timeout = 45000)]
        public async Task Handling_kernel_can_be_specified_as_a_default()
        {
            var receivedOnFakeKernel = new List<IKernelCommand>();

            using var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FakeKernel("fake")
                {
                    Handle = (command, context) =>
                    {
                        receivedOnFakeKernel.Add(command);
                        return Task.CompletedTask;
                    }
                }
            };

            kernel.DefaultKernelName = "fake";

            await kernel.SendAsync(
                new SubmitCode(
                    @"hello!"));

            receivedOnFakeKernel
                .Should()
                .ContainSingle(c => c is SubmitCode)
                .Which
                .As<SubmitCode>()
                .Code
                .Should()
                .Be("hello!");
        }

        [Fact(Timeout = 45000)]
        public async Task Events_published_by_child_kernel_are_visible_in_parent_kernel()
        {
            var subKernel = new CSharpKernel();

            using var compositeKernel = new CompositeKernel
            {
                subKernel
            };

            var events = compositeKernel.KernelEvents.ToSubscribedList();

            await subKernel.SendAsync(new SubmitCode("var x = 1;"));

            events
                .Select(e => e.GetType())
                .Should()
                .ContainInOrder(
                    typeof(CodeSubmissionReceived),
                    typeof(CompleteCodeSubmissionReceived),
                    typeof(CommandHandled));
        }

        [Fact]
        public void Child_kernels_are_disposed_when_CompositeKernel_is_disposed()
        {
            var csharpKernelWasDisposed = false;
            var fsharpKernelWasDisposed = false;

            var csharpKernel = new CSharpKernel();
            csharpKernel.RegisterForDisposal(() => csharpKernelWasDisposed = true);

            var fsharpKernel = new FSharpKernel();
            fsharpKernel.RegisterForDisposal(() => fsharpKernelWasDisposed = true);

            var compositeKernel = new CompositeKernel
            {
                csharpKernel,
                fsharpKernel
            };
            compositeKernel.Dispose();

            csharpKernelWasDisposed.Should().BeTrue();
            fsharpKernelWasDisposed.Should().BeTrue();
        }
    }
}