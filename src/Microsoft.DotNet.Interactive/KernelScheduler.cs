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
        private readonly ConcurrentQueue<ScheduledOperation> _scheduledQueue = new();
        private readonly ConcurrentQueue<ScheduledOperation> _immediateQueue = new();
        private readonly CancellationTokenSource _schedulerDisposalSource = new();
        private readonly ManualResetEventSlim _scheduledOperationMonitor = new(false);
        private readonly object _lockObj = new();
        private readonly Task _runLoopTask;
        private readonly AsyncLocal<ScheduledOperation> _currentTopLevelOperation = new();

        public KernelScheduler()
        {
            _runLoopTask = Task.Factory.StartNew(
                ScheduledOperationRunLoop,
                TaskCreationOptions.LongRunning,
                _schedulerDisposalSource.Token);
        }

        public Task<U> RunAsync(
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
            else if (_currentTopLevelOperation.Value is { })
            {
                operation = new ScheduledOperation(
                    value,
                    onExecuteAsync,
                    null,
                    scope,
                    cancellationToken);
                RunNext(operation);
                // _scheduledOperationMonitor.Set();
            }
            else
            {
                operation = new ScheduledOperation(
                    value,
                    onExecuteAsync,
                    ExecutionContext.Capture(),
                    scope: scope,
                    cancellationToken: cancellationToken);
                _scheduledQueue.Enqueue(operation);
                _scheduledOperationMonitor.Set();
            }

            return operation.TaskCompletionSource.Task;
        }

        private void ScheduledOperationRunLoop(object _)
        {
            // using var __ = KernelSynchronizationContext.Establish(this);

            while (!_schedulerDisposalSource.IsCancellationRequested)
            {
                _scheduledOperationMonitor.Wait(_schedulerDisposalSource.Token);

                while (!_schedulerDisposalSource.IsCancellationRequested &&
                       _scheduledQueue.TryDequeue(out var operation))
                {
                    ExecutionContext executionContext;

                    // FIX: (RunScheduledOperations) 
                    if (_currentTopLevelOperation.Value is {} parentOperation)
                    {
                        executionContext
                         = parentOperation.ExecutionContext;
                    }
                    else
                    {
                        _currentTopLevelOperation.Value = operation;
                        executionContext
                         = operation.ExecutionContext;
                    }

                    if (executionContext is { })
                    {
                        ExecutionContext.Run(
                            executionContext,
                            _ => RunScheduledOperationAndDeferredOperations(operation),
                            operation);
                    }
                    else
                    {
                        RunScheduledOperationAndDeferredOperations(operation);
                    }

                    _currentTopLevelOperation.Value = null;
                }
            }
        }

        private void RunScheduledOperationAndDeferredOperations(ScheduledOperation operation)
        {
            foreach (var deferredOperation in OperationsToRunBefore(operation))
            {
                Run(deferredOperation);
            }

            Run(operation);
        }

        private int _concurrency = 0;

        private void Run(ScheduledOperation operation, bool waitForComplete = false)
        {
            // FIX: (Run) 
            switch (_concurrency)
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

            lock (_lockObj)
            {
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

                    if (!waitForComplete)
                    {
                        operationTask.Wait(_schedulerDisposalSource.Token);
                    }
                    else
                    {
                    }
                }
                catch (Exception exception)
                {
                    operation.TaskCompletionSource.SetException(exception);

                    _scheduledQueue.Clear();
                }
            }
        }

        private void RunNext(ScheduledOperation operation)
        {
            _immediateQueue.Enqueue(operation);
        }

        private IEnumerable<ScheduledOperation> OperationsToRunBefore(
            ScheduledOperation operation)
        {
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

                    while (_immediateQueue.TryDequeue(out var newOperation))
                    {
                        yield return newOperation;
                    }

                    yield return deferredOperation;

                    while (_immediateQueue.TryDequeue(out var newOperation))
                    {
                        yield return newOperation;
                    }
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

            public static IDisposable Establish(KernelScheduler<T, U> scheduler)
            {
                var context = new KernelSynchronizationContext(scheduler);

                SetSynchronizationContext(context);

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
                else if (PreviousContext is { } previous)
                {
                    previous.Post(d, state);
                }
                else
                {
                    Scheduler.RunNext(
                        new ScheduledOperation(
                            default,
                            value =>
                            {
                                Send(d, state);
                                return Task.FromResult(default(U));
                            },
                            default,
                            default));
                }
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                if (PreviousContext is { } previous)
                {
                    previous.Send(d, state);
                }
                else
                {
                    base.Send(d, state);
                }
            }
        }
    }
}