// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Tests.Utility;

using Pocket;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class KernelCommandSchedulerTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        public KernelCommandSchedulerTests(ITestOutputHelper output)
        {
            DisposeAfterTest(output.SubscribeToPocketLogger());
        }

        public void Dispose()
        {
            try
            {
                _disposables?.Dispose();
            }
            catch (Exception ex)
            {
                Logger<KernelCommandSchedulerTests>.Log.Error(exception: ex);
            }
        }

        private void DisposeAfterTest(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        private void DisposeAfterTest(Action action)
        {
            _disposables.Add(action);
        }

        [Fact]
        public async Task command_execute_on_kernel_specified_at_scheduling_time()
        {
            var commandsHandledOnKernel1 = new List<KernelCommand>();
            var commandsHandledOnKernel2 = new List<KernelCommand>();

            var scheduler = new KernelCommandScheduler();

            var kernel1 = new FakeKernel("kernel1")
            {
                Handle = (command, _) =>
                {
                    commandsHandledOnKernel1.Add(command);
                    return Task.CompletedTask;
                }
            };
            var kernel2 = new FakeKernel("kernel2")
            {
                Handle = (command, _) =>
                {
                    commandsHandledOnKernel2.Add(command);
                    return Task.CompletedTask;
                }
            };

            var command1 = new SubmitCode("for kernel 1");
            var command2 = new SubmitCode("for kernel 2");

            await scheduler.Schedule(command1, kernel1);
            await scheduler.Schedule(command2, kernel2);

            commandsHandledOnKernel1.Should().ContainSingle().Which.Should().Be(command1);
            commandsHandledOnKernel2.Should().ContainSingle().Which.Should().Be(command2);
        }

        [Fact]
        public async Task scheduling_a_command_will_defer_deferred_commands_scheduled_on_same_kernel()
        {
            var commandsHandledOnKernel1 = new List<KernelCommand>();

            var scheduler = new KernelCommandScheduler();

            var kernel1 = new FakeKernel("kernel1")
            {
                Handle = (command, _) =>
                {
                    commandsHandledOnKernel1.Add(command);
                    return Task.CompletedTask;
                }
            };
            var kernel2 = new FakeKernel("kernel2")
            {
                Handle = (_, _) => Task.CompletedTask
            };

            var deferredCommand1 = new SubmitCode("deferred for kernel 1");
            var deferredCommand2 = new SubmitCode("deferred for kernel 2");
            var deferredCommand3 = new SubmitCode("deferred for kernel 1");
            var command1 = new SubmitCode("for kernel 1");

            scheduler.DeferCommand(deferredCommand1, kernel1);
            scheduler.DeferCommand(deferredCommand2, kernel2);
            scheduler.DeferCommand(deferredCommand3, kernel1);
            await scheduler.Schedule(command1, kernel1);

            commandsHandledOnKernel1.Should().NotContain(deferredCommand2);
            commandsHandledOnKernel1.Should().BeEquivalentSequenceTo(deferredCommand1, deferredCommand3, command1);
        }

        [Fact]
        public async Task deferred_command_not_executed_are_still_in_deferred_queue()
        {
            var commandsHandledOnKernel1 = new List<KernelCommand>();
            var commandsHandledOnKernel2 = new List<KernelCommand>();

            var scheduler = new KernelCommandScheduler();

            var kernel1 = new FakeKernel("kernel1")
            {
                Handle = (command, _) =>
                {
                    commandsHandledOnKernel1.Add(command);
                    return Task.CompletedTask;
                }
            };
            var kernel2 = new FakeKernel("kernel2")
            {
                Handle = (command, _) =>
                {
                    commandsHandledOnKernel2.Add(command);
                    return Task.CompletedTask;
                }
            };

            var deferredCommand1 = new SubmitCode("deferred for kernel 1");
            var deferredCommand2 = new SubmitCode("deferred for kernel 2");
            var deferredCommand3 = new SubmitCode("deferred for kernel 1");
            var command1 = new SubmitCode("for kernel 1");
            var command2 = new SubmitCode("for kernel 2");

            scheduler.DeferCommand(deferredCommand1, kernel1);
            scheduler.DeferCommand(deferredCommand2, kernel2);
            scheduler.DeferCommand(deferredCommand3, kernel1);
            await scheduler.Schedule(command1, kernel1);

            commandsHandledOnKernel2.Should().BeEmpty();
            commandsHandledOnKernel1.Should().NotContain(deferredCommand2);
            commandsHandledOnKernel1.Should().BeEquivalentSequenceTo(deferredCommand1, deferredCommand3, command1);
            await scheduler.Schedule(command2, kernel2);
            commandsHandledOnKernel2.Should().BeEquivalentSequenceTo(deferredCommand2, command2);
        }

        [Fact]
        public async Task deferred_command_on_parent_kernel_are_executed_when_scheduling_command_on_child_kernel()
        {
            var commandHandledList = new List<(KernelCommand command, Kernel kernel)>();

            var scheduler = new KernelCommandScheduler();

            var childKernel = new FakeKernel("kernel1")
            {
                Handle = (command, context) => command.InvokeAsync(context)
            };
            var parentKernel = new CompositeKernel
            {
                childKernel
            };

            parentKernel.DefaultKernelName = childKernel.Name;

            var deferredCommand1 = new TestCommand((command, context) =>
            {
                commandHandledList.Add((command, context.HandlingKernel));
                return Task.CompletedTask;
            }, childKernel.Name);
            var deferredCommand2 = new TestCommand((command, context) =>
            {
                commandHandledList.Add((command, context.HandlingKernel));
                return Task.CompletedTask;
            }, parentKernel.Name);
            var deferredCommand3 = new TestCommand((command, context) =>
            {
                commandHandledList.Add((command, context.HandlingKernel));
                return Task.CompletedTask;
            }, childKernel.Name);
            var command1 = new TestCommand((command, context) =>
           {
               commandHandledList.Add((command, context.HandlingKernel));
               return Task.CompletedTask;
           }, childKernel.Name);

            scheduler.DeferCommand(deferredCommand1, childKernel);
            scheduler.DeferCommand(deferredCommand2, parentKernel);
            scheduler.DeferCommand(deferredCommand3, childKernel);
            await scheduler.Schedule(command1, childKernel);

            commandHandledList.Select(e => e.command).Should().BeEquivalentSequenceTo(deferredCommand1, deferredCommand2, deferredCommand3, command1);

            commandHandledList.Select(e => e.kernel).Should().BeEquivalentSequenceTo(childKernel, parentKernel, childKernel, childKernel);
        }
    }
}