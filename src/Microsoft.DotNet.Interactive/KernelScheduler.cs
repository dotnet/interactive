// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pocket;

namespace Microsoft.DotNet.Interactive
{
    public class KernelScheduler<T, U> : IDisposable
    {
        private readonly List<DeferredOperation> _deferredOperationSources = new();
        private readonly ConcurrentQueue<ScheduledOperation> _queue = new();
        private readonly Task _loop;
        private readonly CancellationTokenSource _schedulerDisposalSource = new();
        private readonly ManualResetEventSlim _scheduledOperationMonitor = new(false);
        private readonly object _lockObj = new();

        public KernelScheduler()
        {
            _loop = Task.Factory.StartNew(RunScheduledOperations,
                                          TaskCreationOptions.LongRunning,
                                          _schedulerDisposalSource.Token);
        }

        public Task<U> ScheduleAndWaitForCompletionAsync(
            T value,
            OnExecuteDelegate onExecuteAsync,
            string scope = "default",
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            ScheduledOperation operation;
            if (SynchronizationContext.Current is KernelSynchronizationContext ctx)
            {
                operation = new ScheduledOperation(
                    value,
                    onExecuteAsync,
                    null,
                    scope,
                    cancellationToken);
                ctx.Post(_ => Run(operation), operation);
            }
            else
            {
                operation = new ScheduledOperation(
                    value,
                    onExecuteAsync,
                    //  ExecutionContext.Capture(),
                    scope: scope,
                    cancellationToken: cancellationToken);
                _queue.Enqueue(operation);
                _scheduledOperationMonitor.Set();
            }

            return operation.TaskCompletionSource.Task;
        }

        private void RunScheduledOperations(object _)
        {
            while (!_schedulerDisposalSource.IsCancellationRequested)
            {
                _scheduledOperationMonitor.Wait(_schedulerDisposalSource.Token);

                while (!_schedulerDisposalSource.IsCancellationRequested &&
                       _queue.TryDequeue(out var operation))
                {
                    // FIX: (RunScheduledOperations) 
                    using var __ = KernelSynchronizationContext.Establish(
                        this,
                        out var ctx);

                    if (operation.ExecutionContext is { } executionContext)
                    {
                        ExecutionContext.Run(executionContext, DoTheThing, operation);
                    }
                    else
                    {
                        Run(operation);
                    }

                    void DoTheThing(object state)
                    {
                        var deferredOperations = GetDeferredOperationsToRunBefore(operation).ToArray();

                        foreach (var deferredOperation in deferredOperations)
                        {
                            Run(deferredOperation);
                        }

                        Run(operation);
                    }
                }
            }
        }

        private int _concurrency = 0;

        private void Run(ScheduledOperation operation)
        {
            if (_concurrency > 0)
            {
                
            }
            Interlocked.Increment(ref _concurrency);
            using var _ = Disposable.Create(() => Interlocked.Decrement(ref _concurrency));

            try
            {
                var operationTask = operation.ExecuteAsync();

                operationTask.ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        operation.TaskCompletionSource.SetResult(t.Result);
                    }
                    else
                    {
                        operation.TaskCompletionSource.SetException(t.Exception);
                    }
                });

                operationTask.Wait(_schedulerDisposalSource.Token);
            }
            catch (Exception exception)
            {
                operation.TaskCompletionSource.SetException(exception);

                _queue.Clear();
            }


        }

        private IEnumerable<ScheduledOperation> GetDeferredOperationsToRunBefore(
            ScheduledOperation operation)
        {
            // get all deferred operations and pump in
            foreach (var source in _deferredOperationSources)
            {
                foreach (var deferred in source.GetDeferredOperations(
                    operation.Value,
                    operation.Scope))
                {
                    var deferredOperation = new ScheduledOperation(
                        deferred,
                        source.OnExecuteAsync,
                        scope: operation.Scope);

                    yield return deferredOperation;
                }
            }
        }

        public void RegisterDeferredOperationSource(
            GetDeferredOperationsDelegate getDeferredOperations,
            OnExecuteDelegate onExecuteAsync)
        {
            ThrowIfDisposed();

            _deferredOperationSources.Add(new DeferredOperation(onExecuteAsync, getDeferredOperations));
        }

        public void Dispose()
        {
            _schedulerDisposalSource.Cancel();
        }

        private void ThrowIfDisposed()
        {
            if (_schedulerDisposalSource.IsCancellationRequested)
            {
                throw new ObjectDisposedException($"{nameof(KernelScheduler<T, U>)} has been disposed.");
            }
        }

        public delegate Task<U> OnExecuteDelegate(T value);

        public delegate IEnumerable<T> GetDeferredOperationsDelegate(T operationToExecute, string queueName);

        private class ScheduledOperation
        {
            private readonly OnExecuteDelegate _onExecuteAsync;

            public ScheduledOperation(
                T value,
                OnExecuteDelegate onExecuteAsync,
                ExecutionContext executionContext = default,
                string scope = "default",
                CancellationToken cancellationToken = default)
            {
                Value = value;
                ExecutionContext = executionContext;
                _onExecuteAsync = onExecuteAsync;
                Scope = scope;

                TaskCompletionSource = new();

                if (cancellationToken != default)
                {
                    cancellationToken.Register(() => { TaskCompletionSource.SetCanceled(); });
                }
            }

            public TaskCompletionSource<U> TaskCompletionSource { get; }

            public T Value { get; }
            public ExecutionContext ExecutionContext { get; }

            public string Scope { get; }

            public Task<U> ExecuteAsync() => _onExecuteAsync(Value);
        }

        private class DeferredOperation
        {
            public DeferredOperation(OnExecuteDelegate onExecuteAsync, GetDeferredOperationsDelegate getDeferredOperations)
            {
                OnExecuteAsync = onExecuteAsync;
                GetDeferredOperations = getDeferredOperations;
            }

            public GetDeferredOperationsDelegate GetDeferredOperations { get; }

            public OnExecuteDelegate OnExecuteAsync { get; }
        }

        private class KernelSynchronizationContext : SynchronizationContext
        {
            private KernelSynchronizationContext(KernelScheduler<T, U> scheduler)
            {
                Scheduler = scheduler;
                PreviousContext = Current;
            }

            public SynchronizationContext PreviousContext { get; }

            public KernelScheduler<T, U> Scheduler { get; }

            public static IDisposable Establish(
                KernelScheduler<T, U> scheduler,
                out KernelSynchronizationContext ctx)
            {
                var context = new KernelSynchronizationContext(scheduler);

                SetSynchronizationContext(context);

                ctx = context;

                return Disposable.Create(() => { SetSynchronizationContext(context.PreviousContext); });
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                switch (Scheduler._concurrency)
                {
                    case 0: 
                        break;
                    case 1: 
                        break;
                    case 2: 
                        break;
                    default:
                        break;
                }

                if (state is ScheduledOperation operation)
                {
                    


                    if (operation.ExecutionContext is { })
                    {
                        ExecutionContext.Run(
                            operation.ExecutionContext,
                            _ => Scheduler.Run(operation),
                            operation);
                    }
                    else
                    {
                        Scheduler.Run(operation);
                    }
                }
                else if (PreviousContext is not null)
                {
                    PreviousContext.Post(d, state);
                }
                else
                {
                    base.Post(d, state);
                }
            }
        }
    }
}