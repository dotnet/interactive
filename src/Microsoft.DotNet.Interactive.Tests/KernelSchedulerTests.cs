// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.DotNet.Interactive.Tests.Utility;

using Pocket;

using Xunit;
using Xunit.Abstractions;

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
                Logger<KernelSchedulerTests>.Log.Error(exception: ex);
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
            using var scheduler = new KernelScheduler<int, int>();

            var executionList = new List<int>();

            await scheduler.Schedule(1, PerformWork);
            await scheduler.Schedule(2, PerformWork);
            await scheduler.Schedule(3, PerformWork);


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
            var tasks = new Task[3];

            for (var i = 0; i < 3; i++)
            {
                var task = scheduler.Schedule(i, async v =>
                {
                    Interlocked.Increment(ref concurrencyCounter);

                    await Task.Delay(100);
                    maxObservedParallelism = Math.Max(concurrencyCounter, maxObservedParallelism);

                    Interlocked.Decrement(ref concurrencyCounter);
                    return v;
                });
                tasks[i] = task;
            }

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
                await scheduler.Schedule(i, PerformWork);
            }

            executionList.Should().BeEquivalentSequenceTo(10, 1, 20, 20, 2, 30, 30, 30, 3);
        }

        [Fact]
        public void cancel_scheduler_work_prevents_any_scheduled_work_from_executing()
        {
            var executionList = new List<int>();
            using var scheduler = new KernelScheduler<int, int>();
            var barrier = new Barrier(2);
            Task<int> PerformWork(int v)
            {
                barrier.SignalAndWait(5000);
                executionList.Add(v);
                return Task.FromResult(v);
            }

            var scheduledWork = new List<Task>
            {
                scheduler.Schedule(1, PerformWork),
                scheduler.Schedule(2, v =>
                {
                    executionList.Add(v);
                    return Task.FromResult(v);
                }),
                scheduler.Schedule(3, v =>
                {
                    executionList.Add(v);
                    return Task.FromResult(v);
                })
            };

            barrier.SignalAndWait();
            scheduler.Cancel();
            Task.WhenAll(scheduledWork);


            executionList.Should().BeEquivalentTo(1);
        }

        [Fact]
        public async Task cancelled_work_prevents_any_scheduled_work_from_executing()
        {
            var executionList = new List<int>();
            using var scheduler = new KernelScheduler<int, int>();
            var barrier = new Barrier(2);

            async Task<int> PerformWork(int v)
            {
                barrier.SignalAndWait();
                await Task.Delay(3000);
                executionList.Add(v);
                return v;
            }

            var scheduledWork = new List<Task>
            {
                scheduler.Schedule(1, PerformWork),
                scheduler.Schedule(2, v =>
                {
                    executionList.Add(v);
                    return Task.FromResult(v);
                }),
                scheduler.Schedule(3, v =>
                {
                    executionList.Add(v);
                    return Task.FromResult(v);
                })
            };

            barrier.SignalAndWait();
            scheduler.Cancel();
            try
            {
                await Task.WhenAll(scheduledWork);
            }
            catch (TaskCanceledException)
            {

            }

            executionList.Should().BeEmpty();
        }

        [Fact]
        public void cancelling_work_throws_exception()
        {
            var executionList = new List<int>();
            using var scheduler = new KernelScheduler<int, int>();
            var barrier = new Barrier(2);

            async Task<int> PerformWork(int v)
            {
                barrier.SignalAndWait();
                await Task.Delay(3000);
                executionList.Add(v);
                return v;
            }

            var scheduledWork = new List<Task>
            {
                scheduler.Schedule(1, PerformWork),
                scheduler.Schedule(2, v =>
                {
                    executionList.Add(v);
                    return Task.FromResult(v);
                }),
                scheduler.Schedule(3, v =>
                {
                    executionList.Add(v);
                    return Task.FromResult(v);
                })
            };

            barrier.SignalAndWait();
            scheduler.Cancel();
            var operation = new Action( () =>  Task.WhenAll(scheduledWork).Wait(5000));

            operation.Should().Throw<TaskCanceledException>();
        }

        [Fact]
        public async Task exception_in_scheduled_work_halts_execution()
        {
            var executionList = new List<int>();
            using var scheduler = new KernelScheduler<int, int>();
            var barrier = new Barrier(2);

            Task<int> PerformWork(int v)
            {
                barrier.SignalAndWait();
                throw new InvalidOperationException("test exception");
            }

            var scheduledWork = new List<Task>
            {
                scheduler.Schedule(1, PerformWork),
                scheduler.Schedule(2, v =>
                {
                    executionList.Add(v);
                    return Task.FromResult(v);
                }),
                scheduler.Schedule(3, v =>
                {
                    executionList.Add(v);
                    return Task.FromResult(v);
                })
            };

            barrier.SignalAndWait();
            try
            {
                await Task.WhenAll(scheduledWork);
            }
            catch(InvalidOperationException)
            {

            }

            executionList.Should().BeEmpty();
        }

        [Fact]
        public void exception_in_scheduled_work_is_propagated()
        {
            var executionList = new List<int>();
            using var scheduler = new KernelScheduler<int, int>();
            var barrier = new Barrier(2);

            Task<int> PerformWork(int v)
            {
                barrier.SignalAndWait();
                throw new InvalidOperationException("test exception");
            }

            var scheduledWork = new List<Task>
            {
                scheduler.Schedule(1, PerformWork),
                scheduler.Schedule(2, v =>
                {
                    executionList.Add(v);
                    return Task.FromResult(v);
                }),
                scheduler.Schedule(3, v =>
                {
                    executionList.Add(v);
                    return Task.FromResult(v);
                })
            };

            barrier.SignalAndWait();
            var operation = new Action(() => Task.WhenAll(scheduledWork).Wait(5000));

            operation.Should().Throw<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be("test exception");
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

            await scheduler.Schedule(1, PerformWorkAsync);
            await scheduler.Schedule(2, PerformWorkAsync);

            _ = scheduler.Schedule(3, PerformWorkAsync);

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
                await scheduler.Schedule(i, PerformWork, $"scope{i}");
            }

            executionList.Should().BeEquivalentSequenceTo(1, 20, 20, 2, 3);
        }
    }
}