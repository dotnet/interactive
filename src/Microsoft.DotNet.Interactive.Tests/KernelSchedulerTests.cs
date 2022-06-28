// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.Utility;
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

            await scheduler.RunAsync(1, PerformWork);
            await scheduler.RunAsync(2, PerformWork);
            await scheduler.RunAsync(3, PerformWork);

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
                return scheduler.RunAsync(i, async v =>
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
                (v, _) => Enumerable.Repeat(v * 10, v).ToList(), PerformWork);

            for (var i = 1; i <= 3; i++)
            {
                await scheduler.RunAsync(i, PerformWork);
            }

            executionList.Should().BeEquivalentSequenceTo(10, 1, 20, 20, 2, 30, 30, 30, 3);
        }

        [Fact]
        public async Task Deferred_work_in_progress_is_allowed_to_complete_when_the_work_that_triggered_it_is_cancelled()
        {
            using var scheduler = new KernelScheduler<int, int>();
            var cts = new CancellationTokenSource();

            var deferredOperations = new[] { 1, 2, 3 };
            var completedDeferredOperations = new List<int>();

            scheduler.RegisterDeferredOperationSource(
                (_, _) => deferredOperations, 
              async i =>
              {
                  if (!cts.IsCancellationRequested)
                  {
                      cts.Cancel();
                  }

                  await Task.Delay(50);

                  completedDeferredOperations.Add(i);
                  return i;
              });

            var run = () => scheduler.RunAsync(4, Task.FromResult, cancellationToken: cts.Token);

            await run.Invoking(async r => await r())
                .Should()
                .ThrowAsync<OperationCanceledException>();

            await Task.Delay(1000);
            
            completedDeferredOperations
                .Should()
                .BeEquivalentTo(deferredOperations);
        }

        [Fact]
        public void disposing_scheduler_prevents_later_scheduled_work_from_executing()
        {
            using var scheduler = new KernelScheduler<int, int>();
            var barrier = new Barrier(2);
            var laterWorkWasExecuted = false;

            var t1 = scheduler.RunAsync(1, async v =>
            {
                barrier.SignalAndWait();
                await Task.Delay(3000);
                return v;
            });
            var t2 = scheduler.RunAsync(2, v =>
            {
                laterWorkWasExecuted = true;
                return Task.FromResult(v);
            });

            barrier.SignalAndWait();
            scheduler.Dispose();

            t2.Status.Should().Be(TaskStatus.WaitingForActivation);
            laterWorkWasExecuted.Should().BeFalse();
        }

        [FactSkipLinux]
        public void cancelling_work_in_progress_prevents_subsequent_work_scheduled_before_cancellation_from_executing()
        {
            using var scheduler = new KernelScheduler<int, int>();
            var cts = new CancellationTokenSource();
            var barrier = new Barrier(2);
            var laterWorkWasExecuted = false;

            // schedule work to be cancelled
            var _ = scheduler.RunAsync(1, async v =>
            {
                barrier.SignalAndWait();
                await Task.Delay(5000);
                return v;
            }, cancellationToken: cts.Token);

            // schedule more work prior to cancellation
            var laterWork = scheduler.RunAsync(2, v =>
            {
                laterWorkWasExecuted = true;
                return Task.FromResult(v);
            });

            barrier.SignalAndWait();
            cts.Cancel();

            laterWork.Status.Should().Be(TaskStatus.WaitingForActivation);
            laterWorkWasExecuted.Should().BeFalse();
        }

        [Fact]
        public void work_in_progress_can_be_cancelled_without_holding_reference_to_the_CancellationToken_used_to_schedule_it()
        {
            using var scheduler = new KernelScheduler<int, int>();
            var barrier = new Barrier(2);
            var cts = new CancellationTokenSource();

            var task = scheduler.RunAsync(1, async v =>
            {
                barrier.SignalAndWait(5000);
                await Task.Delay(1000);
                return v;
            }, cancellationToken: cts.Token);

            barrier.SignalAndWait(5000);
            scheduler.CancelCurrentOperation();

            task.IsCanceled.Should().BeTrue();
        }

        [Fact]
        public void cancelling_work_in_progress_throws_exception()
        {
            using var scheduler = new KernelScheduler<int, int>();
            var cts = new CancellationTokenSource();

            var barrier = new Barrier(2);

            var work = scheduler.RunAsync(1, async v =>
            {
                barrier.SignalAndWait();
                await Task.Delay(3000);
                return v;
            }, cancellationToken: cts.Token);

            barrier.SignalAndWait();
            cts.Cancel();

            work.Invoking(async w => await w)
                .Should()
                .ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public void disposing_scheduler_throws_exception()
        {
            using var scheduler = new KernelScheduler<int, int>();

            var barrier = new Barrier(2);

            var work = scheduler.RunAsync(1, async v =>
            {
                barrier.SignalAndWait();
                await Task.Delay(3000);
                return v;
            });

            barrier.SignalAndWait();
            scheduler.Dispose();

            work.Invoking(async w => await w)
                .Should()
                .ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task exception_in_scheduled_work_does_not_prevent_execution_of_work_already_queued()
        {
            using var scheduler = new KernelScheduler<int, int>();
            var barrier = new Barrier(2);
            var laterWorkWasExecuted = false;

            var t1 = scheduler.RunAsync(1, _ =>
            {
                barrier.SignalAndWait();
                throw new DataMisalignedException();
            });
            var t2 = scheduler.RunAsync(2, v =>
            {
                laterWorkWasExecuted = true;
                return Task.FromResult(v);
            });

            barrier.SignalAndWait();

            await t2;
            laterWorkWasExecuted.Should().BeTrue();
        }

        [Fact]
        public async Task after_an_exception_in_scheduled_work_more_work_can_be_scheduled()
        {
            using var scheduler = new KernelScheduler<int, int>();

            try
            {
                await scheduler.RunAsync(1, _ => throw new DataMisalignedException());
            }
            catch (DataMisalignedException)
            {
            }

            var next = await scheduler.RunAsync(2, _ => Task.FromResult(2));

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

            var work = scheduler.RunAsync(1, Throw);

            barrier.SignalAndWait();

            work.Invoking(async w => await w)
                .Should()
                .ThrowAsync<DataMisalignedException>();
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

            var one = scheduler.RunAsync(1, PerformWorkAsync);
            var two = scheduler.RunAsync(2, PerformWorkAsync);
            var three = scheduler.RunAsync(3, PerformWorkAsync);

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
                (v, scope) => scope == "scope2" ? Enumerable.Repeat(v * 10, v).ToList() : Enumerable.Empty<int>().ToList(), PerformWork);

            for (var i = 1; i <= 3; i++)
            {
                await scheduler.RunAsync(i, PerformWork, $"scope{i}");
            }

            executionList.Should().BeEquivalentSequenceTo(1, 20, 20, 2, 3);
        }

        [Fact]
        public async Task work_can_be_scheduled_from_within_scheduled_work()
        {
            var executionList = new List<string>();

            using var scheduler = new KernelScheduler<string, string>();

            await scheduler.RunAsync("outer", async _ =>
            {
                executionList.Add("outer 1");

                await scheduler.RunAsync("inner", async _ =>
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
        public async Task concurrent_schedulers_do_not_interfere_with_one_another()
        {
            var participantCount = 5;
            var barrier = new Barrier(participantCount);
            var schedulers = Enumerable.Range(0, participantCount)
                                       .Select(_ => new KernelScheduler<int, int>());

            var tasks = schedulers.Select((s, i) => s.RunAsync(i, async value =>
                                              {
                                                  await Task.Yield();
                                                  barrier.SignalAndWait();
                                                  return value;
                                              }
                                          ));

            var xs = await Task.WhenAll(tasks);

            xs.Should().BeEquivalentTo(new []{0, 1, 2, 3, 4});
        }

        [Fact]
        public async Task AsyncContext_is_maintained_across_async_operations_within_scheduled_work()
        {
            using var scheduler = new KernelScheduler<int, int>();
            int asyncId1 = default;
            int asyncId2 = default;

            await scheduler.RunAsync(0, async value =>
            {
                AsyncContext.TryEstablish(out asyncId1);
                await Task.Yield();
                AsyncContext.TryEstablish(out asyncId2);
                return value;
            });

            asyncId2.Should().Be(asyncId1);
        }

        [Fact]
        public async Task AsyncContext_is_maintained_across_from_outside_context_to_scheduled_work()
        {
            using var scheduler = new KernelScheduler<int, int>();
            int asyncId1 = default;
            int asyncId2 = default;
            
            AsyncContext.TryEstablish(out asyncId1);

            await scheduler.RunAsync(0, async value =>
            {
                await Task.Yield();
                AsyncContext.TryEstablish(out asyncId2);
                return value;
            });

            asyncId2.Should().Be(asyncId1);
        }
              
        [Fact]
        public async Task AsyncContext_does_not_leak_from_inner_context()
        {
            using var scheduler = new KernelScheduler<int, int>();
            int asyncId = default;

            await scheduler.RunAsync(0, async value =>
            {
                await Task.Yield();
                AsyncContext.TryEstablish(out asyncId);
                return value;
            });

            AsyncContext.Id.Should().NotBe(asyncId);
        }

        [Fact]
        public async Task AsyncContext_does_not_leak_between_scheduled_work()
        {
            using var scheduler = new KernelScheduler<int, int>();
            int asyncId1 = default;
            int asyncId2 = default;

            await scheduler.RunAsync(1, async value =>
            {
                AsyncContext.TryEstablish(out asyncId1);
                await Task.Yield();
                return value;
            });
            await scheduler.RunAsync(2, async value =>
            {
                AsyncContext.TryEstablish(out asyncId2);
                await Task.Yield();
                return value;
            });

            asyncId2.Should().NotBe(asyncId1);
        }

        [Fact]
        public async Task AsyncContext_flows_from_scheduled_work_to_deferred_work()
        {
            using var scheduler = new KernelScheduler<int, int>();
            int asyncIdForScheduledWork = default;
            var asyncIdsForDeferredWork = new ConcurrentBag<int>();

            AsyncContext.TryEstablish(out var _);

            scheduler.RegisterDeferredOperationSource((execute, name) =>
            {
                return new[] {0,1,2};
            }, async value =>
            {
                AsyncContext.TryEstablish(out var asyncId);
                asyncIdsForDeferredWork.Add( asyncId );
                await Task.Yield();
                return value;
            });

            await scheduler.RunAsync(3, async value =>
            {
                AsyncContext.TryEstablish(out asyncIdForScheduledWork);
                await Task.Yield();
                return value;
            });

            asyncIdsForDeferredWork.Should()
                                   .HaveCount(3)
                                   .And
                                   .AllBeEquivalentTo(asyncIdForScheduledWork);
        }
        
        [Fact]
        public async Task AsyncContext_does_not_leak_between_scheduled_work_when_ExecutionContext_is_suppressed()
        {
            ExecutionContext.SuppressFlow();
            
            await AsyncContext_does_not_leak_between_scheduled_work();
        }

        [Fact]
        public async Task work_can_be_scheduled_from_within_scheduled_work_when_ExecutionContext_is_suppressed()
        {
            ExecutionContext.SuppressFlow();
            
            await work_can_be_scheduled_from_within_scheduled_work();
        }

        [Fact]
        public async Task concurrent_schedulers_do_not_interfere_with_one_another_when_ExecutionContext_is_suppressed()
        {
            ExecutionContext.SuppressFlow();
            
            await concurrent_schedulers_do_not_interfere_with_one_another();
        }

        [Fact]
        public async Task AsyncContext_is_maintained_across_async_operations_within_scheduled_work_when_ExecutionContext_is_suppressed()
        {
            ExecutionContext.SuppressFlow();
            
            await AsyncContext_is_maintained_across_async_operations_within_scheduled_work();
        }

        [Fact]
        public async Task AsyncContext_does_not_leak_from_inner_context_when_ExecutionContext_is_suppressed()
        {
            ExecutionContext.SuppressFlow();
            
            await AsyncContext_does_not_leak_from_inner_context();
        }
    }
}