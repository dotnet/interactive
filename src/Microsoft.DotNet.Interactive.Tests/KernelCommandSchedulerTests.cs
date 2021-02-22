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
        public async Task scheduled_work_is_completed_in_order()
        {
            var scheduler = new KernelScheduler<int, int>();

            var executionList = new List<int>();

            await scheduler.Schedule(1, (v) => executionList.Add(v));
            await scheduler.Schedule(2, (v) => executionList.Add(v));
            await scheduler.Schedule(3, (v) => executionList.Add(v));


            executionList.Should().BeEquivalentSequenceTo(1, 2, 3);
        }

        [Fact]
        public void scheduled_work_does_not_execute_in_parallel()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task deferred_work_is_executed_before_new_work()
        {
            var executionList = new List<int>();

            var scheduler = new KernelScheduler<int, int>();
            scheduler.RegisterDeferredOperationSource(
                v => Enumerable.Repeat(v * 10, v), (v) => executionList.Add(v));

            await scheduler.Schedule(1, (v) => executionList.Add(v));
            await scheduler.Schedule(2, (v) => executionList.Add(v));
            await scheduler.Schedule(3, (v) => executionList.Add(v));

            executionList.Should().BeEquivalentSequenceTo(10,1,20,20, 2,30,30,30, 3);
        }

        [Fact]
        public async Task cancel_scheduler_operation_prevents_execution()
        {
            var scheduler = new KernelScheduler<int, int>();
            scheduler.RegisterDeferredOperationSource(
                v => Enumerable.Repeat(v * 10, v), async (v) => await Task.Delay(1000));

            var t1 = scheduler.Schedule(1, (v) => Task.Delay(10000));
            var t2 = scheduler.Schedule(2, (v) => Task.Delay(10000));
            var t3 = scheduler.Schedule(3, (v) => Task.Delay(10000));

            await Task.Delay(100);

            scheduler.Cancel();
            Exception exception = null;
            try
            {
                await Task.WhenAll(t1, t2, t3);
            }
            catch (Exception e)
            {
                exception = e;
            }

            exception.Should().NotBeNull();
        }

        [Fact]
        public async Task awaiting_one_operation_does_not_wait_all()
        {
            var executionList = new List<int>();

            var scheduler = new KernelScheduler<int, int>();


            await scheduler.Schedule(1, async (v) =>
            {
                await Task.Delay(10);
                executionList.Add(v);
            });
            await scheduler.Schedule(2, async (v) =>
            {
                await Task.Delay(10);
                executionList.Add(v);
            });

            _ = scheduler.Schedule(3, async (v) =>
              {
                  await Task.Delay(200);
                  executionList.Add(v);
              });

            executionList.Should().BeEquivalentSequenceTo( 1, 2);
        }

        [Fact]
        public void new_work_is_executed_after_all_require()
        {
            throw new NotImplementedException();
        }

        //[Fact]
        //public async Task command_execute_on_kernel_specified_at_scheduling_time()
        //{
        //    var commandsHandledOnKernel1 = new List<KernelCommand>();
        //    var commandsHandledOnKernel2 = new List<KernelCommand>();

        //    var scheduler = new KernelCommandScheduler();

        //    var kernel1 = new FakeKernel("kernel1")
        //    {
        //        Handle = (command, _) =>
        //        {
        //            commandsHandledOnKernel1.Add(command);
        //            return Task.CompletedTask;
        //        }
        //    };
        //    var kernel2 = new FakeKernel("kernel2")
        //    {
        //        Handle = (command, _) =>
        //        {
        //            commandsHandledOnKernel2.Add(command);
        //            return Task.CompletedTask;
        //        }
        //    };

        //    var command1 = new SubmitCode("for kernel 1", kernel1.Name);
        //    var command2 = new SubmitCode("for kernel 2", kernel2.Name);

        //    await scheduler.Schedule(command1);
        //    await scheduler.Schedule(command2);

        //    commandsHandledOnKernel1.Should().ContainSingle().Which.Should().Be(command1);
        //    commandsHandledOnKernel2.Should().ContainSingle().Which.Should().Be(command2);
        //}

        //[Fact]
        //public async Task scheduling_a_command_will_defer_deferred_commands_scheduled_on_same_kernel()
        //{
        //    var commandsHandledOnKernel1 = new List<KernelCommand>();

        //    var scheduler = new KernelCommandScheduler();

        //    var kernel1 = new FakeKernel("kernel1")
        //    {
        //        Handle = (command, _) =>
        //        {
        //            commandsHandledOnKernel1.Add(command);
        //            return Task.CompletedTask;
        //        }
        //    };
        //    var kernel2 = new FakeKernel("kernel2")
        //    {
        //        Handle = (_, _) => Task.CompletedTask
        //    };

        //    var deferredCommand1 = new SubmitCode("deferred for kernel 1", kernel1.Name);
        //    var deferredCommand2 = new SubmitCode("deferred for kernel 2", kernel2.Name);
        //    var deferredCommand3 = new SubmitCode("deferred for kernel 1", kernel1.Name);
        //    var command1 = new SubmitCode("for kernel 1", kernel1.Name);

        //    scheduler.DeferCommand(deferredCommand1);
        //    scheduler.DeferCommand(deferredCommand2);
        //    scheduler.DeferCommand(deferredCommand3);
        //    await scheduler.Schedule(command1);

        //    commandsHandledOnKernel1.Should().NotContain(deferredCommand2);
        //    commandsHandledOnKernel1.Should().BeEquivalentSequenceTo(deferredCommand1, deferredCommand3, command1);
        //}

        //[Fact]
        //public async Task deferred_command_not_executed_are_still_in_deferred_queue()
        //{
        //    var commandsHandledOnKernel1 = new List<KernelCommand>();
        //    var commandsHandledOnKernel2 = new List<KernelCommand>();

        //    var scheduler = new KernelCommandScheduler();

        //    var kernel1 = new FakeKernel("kernel1")
        //    {
        //        Handle = (command, _) =>
        //        {
        //            commandsHandledOnKernel1.Add(command);
        //            return Task.CompletedTask;
        //        }
        //    };
        //    var kernel2 = new FakeKernel("kernel2")
        //    {
        //        Handle = (command, _) =>
        //        {
        //            commandsHandledOnKernel2.Add(command);
        //            return Task.CompletedTask;
        //        }
        //    };

        //    var deferredCommand1 = new SubmitCode("deferred for kernel 1");
        //    var deferredCommand2 = new SubmitCode("deferred for kernel 2");
        //    var deferredCommand3 = new SubmitCode("deferred for kernel 1");
        //    var command1 = new SubmitCode("for kernel 1");
        //    var command2 = new SubmitCode("for kernel 2");

        //    scheduler.DeferCommand(deferredCommand1, kernel1);
        //    scheduler.DeferCommand(deferredCommand2, kernel2);
        //    scheduler.DeferCommand(deferredCommand3, kernel1);
        //    await scheduler.Schedule(command1, kernel1);

        //    commandsHandledOnKernel2.Should().BeEmpty();
        //    commandsHandledOnKernel1.Should().NotContain(deferredCommand2);
        //    commandsHandledOnKernel1.Should().BeEquivalentSequenceTo(deferredCommand1, deferredCommand3, command1);
        //    await scheduler.Schedule(command2, kernel2);
        //    commandsHandledOnKernel2.Should().BeEquivalentSequenceTo(deferredCommand2, command2);
        //}

        //[Fact]
        //public async Task deferred_command_on_parent_kernel_are_executed_when_scheduling_command_on_child_kernel()
        //{
        //    var commandHandledList = new List<(KernelCommand command, Kernel kernel)>();

        //    var scheduler = new KernelCommandScheduler();

        //    var childKernel = new FakeKernel("kernel1")
        //    {
        //        Handle = (command, context) => command.InvokeAsync(context)
        //    };
        //    var parentKernel = new CompositeKernel
        //    {
        //        childKernel
        //    };

        //    parentKernel.DefaultKernelName = childKernel.Name;

        //    var deferredCommand1 = new TestCommand((command, context) =>
        //    {
        //        commandHandledList.Add((command, context.HandlingKernel));
        //        return Task.CompletedTask;
        //    }, childKernel.Name);
        //    var deferredCommand2 = new TestCommand((command, context) =>
        //    {
        //        commandHandledList.Add((command, context.HandlingKernel));
        //        return Task.CompletedTask;
        //    }, parentKernel.Name);
        //    var deferredCommand3 = new TestCommand((command, context) =>
        //    {
        //        commandHandledList.Add((command, context.HandlingKernel));
        //        return Task.CompletedTask;
        //    }, childKernel.Name);
        //    var command1 = new TestCommand((command, context) =>
        //   {
        //       commandHandledList.Add((command, context.HandlingKernel));
        //       return Task.CompletedTask;
        //   }, childKernel.Name);

        //    scheduler.DeferCommand(deferredCommand1, childKernel);
        //    scheduler.DeferCommand(deferredCommand2, parentKernel);
        //    scheduler.DeferCommand(deferredCommand3, childKernel);
        //    await scheduler.Schedule(command1, childKernel);

        //    commandHandledList.Select(e => e.command).Should().BeEquivalentSequenceTo(deferredCommand1, deferredCommand2, deferredCommand3, command1);

        //    commandHandledList.Select(e => e.kernel).Should().BeEquivalentSequenceTo(childKernel, parentKernel, childKernel, childKernel);
        //}
    }
    }