// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Xunit;
using Xunit.Abstractions;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Tests.KernelSchedulerTests>;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class KernelSchedulerTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        public KernelSchedulerTests(ITestOutputHelper output)
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
                Log.Error(exception: ex);
            }
        }

        private void DisposeAfterTest(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        [Fact]
        public async Task scheduled_work_is_completed_in_order()
        {
            using var scheduler = new KernelScheduler<int, int>();

            var executionList = new List<int>();

            await scheduler.ScheduleAndWaitForCompletionAsync(1, PerformWork);
            await scheduler.ScheduleAndWaitForCompletionAsync(2, PerformWork);
            await scheduler.ScheduleAndWaitForCompletionAsync(3, PerformWork);

            executionList.Should().BeEquivalentSequenceTo(1, 2, 3);

            Task<int> PerformWork(int v)
            {
                executionList.Add(v);
                return Task.FromResult(v);
            }
        }

        [Fact]
        public async Task scheduled_work_does_not_execute_in_parallel()
        {
            using var scheduler = new KernelScheduler<int, int>();
            var concurrencyCounter = 0;
            var maxObservedParallelism = 0;

            var tasks = Enumerable.Range(1, 3).Select(i =>
            {
                return scheduler.ScheduleAndWaitForCompletionAsync(i, async v =>
                {
                    Interlocked.Increment(ref concurrencyCounter);

                    await Task.Delay(100);
                    maxObservedParallelism = Math.Max(concurrencyCounter, maxObservedParallelism);

                    Interlocked.Decrement(ref concurrencyCounter);
                    return v;
                });
            });

            await Task.WhenAll(tasks);

            maxObservedParallelism.Should().Be(1);
        }

        [Fact]
        public async Task deferred_work_is_executed_before_new_work()
        {
            var executionList = new List<int>();

            Task<int> PerformWork(int v)
            {
                executionList.Add(v);
                return Task.FromResult(v);
            }

            using var scheduler = new KernelScheduler<int, int>();
            scheduler.RegisterDeferredOperationSource(
                (v, _) => Enumerable.Repeat(v * 10, v), PerformWork);

            for (var i = 1; i <= 3; i++)
            {
                await scheduler.ScheduleAndWaitForCompletionAsync(i, PerformWork);
            }

            executionList.Should().BeEquivalentSequenceTo(10, 1, 20, 20, 2, 30, 30, 30, 3);
        }

        [Fact]
        public void disposing_scheduler_prevents_later_scheduled_work_from_executing()
        {
            using var scheduler = new KernelScheduler<int, int>();
            var barrier = new Barrier(2);
            var laterWorkWasExecuted = false;

            var t1 = scheduler.ScheduleAndWaitForCompletionAsync(1, async v =>
            {
                barrier.SignalAndWait();
                await Task.Delay(3000);
                return v;
            });
            var t2 = scheduler.ScheduleAndWaitForCompletionAsync(2, v =>
            {
                laterWorkWasExecuted = true;
                return Task.FromResult(v);
            });

            barrier.SignalAndWait();
            scheduler.Dispose();

            t2.Status.Should().Be(TaskStatus.WaitingForActivation);
            laterWorkWasExecuted.Should().BeFalse();
        }

        [Fact]
        public void cancelling_work_in_progress_prevents_later_scheduled_work_from_executing()
        {
            using var scheduler = new KernelScheduler<int, int>();
            var cts = new CancellationTokenSource();
            var barrier = new Barrier(2);
            var laterWorkWasExecuted = false;

            var t1 = scheduler.ScheduleAndWaitForCompletionAsync(1, async v =>
            {
                barrier.SignalAndWait();
                await Task.Delay(3000);
                return v;
            }, cancellationToken: cts.Token);

            var t2 = scheduler.ScheduleAndWaitForCompletionAsync(2, v =>
            {
                laterWorkWasExecuted = true;
                return Task.FromResult(v);
            });

            barrier.SignalAndWait();
            cts.Cancel();

            t2.Status.Should().Be(TaskStatus.WaitingForActivation);
            laterWorkWasExecuted.Should().BeFalse();
        }

        [Fact]
        public void cancelling_work_in_progress_throws_exception()
        {
            using var scheduler = new KernelScheduler<int, int>();
            var cts = new CancellationTokenSource();

            var barrier = new Barrier(2);

            var work = scheduler.ScheduleAndWaitForCompletionAsync(1, async v =>
            {
                barrier.SignalAndWait();
                await Task.Delay(3000);
                return v;
            }, cancellationToken: cts.Token);

            barrier.SignalAndWait();
            cts.Cancel();

            work.Invoking(async w => await w)
                .Should()
                .Throw<OperationCanceledException>();
        }

        [Fact]
        public void disposing_scheduler_throws_exception()
        {
            using var scheduler = new KernelScheduler<int, int>();

            var barrier = new Barrier(2);

            var work = scheduler.ScheduleAndWaitForCompletionAsync(1, async v =>
            {
                barrier.SignalAndWait();
                await Task.Delay(3000);
                return v;
            });

            barrier.SignalAndWait();
            scheduler.Dispose();

            work.Invoking(async w => await w)
                .Should()
                .Throw<OperationCanceledException>();
        }

        [Fact]
        public void exception_in_scheduled_work_halts_execution_of_work_already_queued()
        {
            using var scheduler = new KernelScheduler<int, int>();
            var barrier = new Barrier(2);
            var laterWorkWasExecuted = false;

            var t1 = scheduler.ScheduleAndWaitForCompletionAsync(1, _ =>
            {
                barrier.SignalAndWait();
                throw new DataMisalignedException();
            });
            var t2 = scheduler.ScheduleAndWaitForCompletionAsync(2, v =>
            {
                laterWorkWasExecuted = true;
                return Task.FromResult(v);
            });

            barrier.SignalAndWait();

            t2.Status.Should().Be(TaskStatus.WaitingForActivation);
            laterWorkWasExecuted.Should().BeFalse();
        }

        [Fact]
        public async Task after_an_exception_in_scheduled_work_more_work_can_be_scheduled()
        {
            using var scheduler = new KernelScheduler<int, int>();

            try
            {
                await scheduler.ScheduleAndWaitForCompletionAsync(1, _ => throw new DataMisalignedException());
            }
            catch (DataMisalignedException)
            {
            }

            var next = await scheduler.ScheduleAndWaitForCompletionAsync(2, _ => Task.FromResult(2));

            next.Should().Be(2);
        }

        [Fact]
        public void exception_in_scheduled_work_is_propagated()
        {
            using var scheduler = new KernelScheduler<int, int>();
            var barrier = new Barrier(2);

            Task<int> Throw(int v)
            {
                barrier.SignalAndWait();
                throw new DataMisalignedException();
            }

            var work = scheduler.ScheduleAndWaitForCompletionAsync(1, Throw);

            barrier.SignalAndWait();

            work.Invoking(async w => await w)
                .Should()
                .Throw<DataMisalignedException>();
        }

        [Fact]
        public async Task awaiting_for_work_to_complete_does_not_wait_for_subsequent_work()
        {
            var executionList = new List<int>();

            using var scheduler = new KernelScheduler<int, int>();

            async Task<int> PerformWorkAsync(int v)
            {
                await Task.Delay(200);
                executionList.Add(v);
                return v;
            }

            var one = scheduler.ScheduleAndWaitForCompletionAsync(1, PerformWorkAsync);
            var two = scheduler.ScheduleAndWaitForCompletionAsync(2, PerformWorkAsync);
            var three = scheduler.ScheduleAndWaitForCompletionAsync(3, PerformWorkAsync);

            await two;

            executionList.Should().BeEquivalentSequenceTo(1, 2);
        }

        [Fact]
        public async Task deferred_work_is_done_based_on_the_scope_of_scheduled_work()
        {
            var executionList = new List<int>();

            Task<int> PerformWork(int v)
            {
                executionList.Add(v);
                return Task.FromResult(v);
            }

            using var scheduler = new KernelScheduler<int, int>();
            scheduler.RegisterDeferredOperationSource(
                (v, scope) => scope == "scope2" ? Enumerable.Repeat(v * 10, v) : Enumerable.Empty<int>(), PerformWork);

            for (var i = 1; i <= 3; i++)
            {
                await scheduler.ScheduleAndWaitForCompletionAsync(i, PerformWork, $"scope{i}");
            }

            executionList.Should().BeEquivalentSequenceTo(1, 20, 20, 2, 3);
        }

        [Fact]
        public async Task work_can_be_scheduled_from_within_scheduled_work()
        {
            var executionList = new List<string>();

            using var scheduler = new KernelScheduler<string, string>();

            await scheduler.ScheduleAndWaitForCompletionAsync("outer", async _ =>
            {
                executionList.Add("outer 1");

                await scheduler.ScheduleAndWaitForCompletionAsync("inner", async _ =>
                {
                    executionList.Add("inner 1");
                    await Task.Yield();
                    executionList.Add("inner 2");
                    return default;
                });

                executionList.Add("outer 2");

                return default;
            });

            executionList.Should()
                         .BeEquivalentSequenceTo(
                             "outer 1",
                             "inner 1",
                             "inner 2",
                             "outer 2"
                         );
        }

        [Fact]
        public async Task schedulers_do_not_interfere_with_one_another()
        {
            var participantCount = 5;
            var barrier = new Barrier(participantCount);
            var schedulers = Enumerable.Range(0, participantCount)
                                       .Select(_ => new KernelScheduler<int, int>());

            var tasks = schedulers.Select((s, i) => s.ScheduleAndWaitForCompletionAsync(i, async value =>
                                              {
                                                  barrier.SignalAndWait();
                                                  return value;
                                              }
                                          ));

            var xs = await Task.WhenAll(tasks);

            xs.Should().BeEquivalentTo(0, 1, 2, 3, 4);
        }
    }
}