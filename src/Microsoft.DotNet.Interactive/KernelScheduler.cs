// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pocket;

namespace Microsoft.DotNet.Interactive
{
    public class KernelScheduler<T, U> : IDisposable
    {
        private readonly List<DeferredOperation> _deferredOperationSources = new();
        private readonly ConcurrentQueue<ScheduledOperation> _immediateQueue = new();
        private readonly CancellationTokenSource _schedulerDisposalSource = new();
        private readonly Task _runLoopTask;
        private readonly AsyncLocal<ScheduledOperation> _currentTopLevelOperation = new();
        private readonly Logger Log = new("KernelScheduler");

        private readonly BlockingCollection<ScheduledOperation> _topLevelScheduledOperations = new();
        private readonly Barrier _barrier = new(2);

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
            if (_currentTopLevelOperation.Value is not { })
            {
                operation = new ScheduledOperation(
                    value,
                    onExecuteAsync,
                    ExecutionContext.Capture(),
                    scope: scope,
                    cancellationToken: cancellationToken);
                _topLevelScheduledOperations.Add(operation);
            }
            else
            {
                // recursive scheduling
                operation = new ScheduledOperation(
                    value,
                    onExecuteAsync,
                    null,
                    scope,
                    cancellationToken);
                RunPreemptively(operation);
            }

            return operation.TaskCompletionSource.Task;
        }

        private void ScheduledOperationRunLoop(object _)
        {
            foreach (var operation in _topLevelScheduledOperations.GetConsumingEnumerable(_schedulerDisposalSource.Token))
            {
                ExecutionContext executionContext;

                if (_currentTopLevelOperation.Value is { } parentOperation)
                {
                    executionContext = parentOperation.ExecutionContext;
                }
                else
                {
                    _currentTopLevelOperation.Value = operation;
                    executionContext = operation.ExecutionContext;
                }

                ExecutionContext.Run(
                    executionContext,
                _ => RunScheduledOperationAndDeferredOperations(operation),
                operation);

                operation.TaskCompletionSource.Task.ContinueWith(_ =>
                {
                    _barrier.SignalAndWait(_schedulerDisposalSource.Token);
                });
                _barrier.SignalAndWait(_schedulerDisposalSource.Token);

                _currentTopLevelOperation.Value = null;
            }
        }

        private void Run(ScheduledOperation operation)
        {
            #region debuggy stuff
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

            if (_currentTopLevelOperation.Value is { })
            {
            }
            else
            {
                _currentTopLevelOperation.Value = operation;
            }

            Interlocked.Increment(ref _concurrency);
            using var _ = Disposable.Create(() => Interlocked.Decrement(ref _concurrency));
            #endregion

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
                }).Wait(_schedulerDisposalSource.Token);
            }
            catch (Exception exception)
            {
                if (!operation.TaskCompletionSource.Task.IsCompleted)
                {
                    operation.TaskCompletionSource.SetException(exception);
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

        private void RunPreemptively(ScheduledOperation operation)
        {
            Run(operation);
            operation.TaskCompletionSource.Task.Wait(_schedulerDisposalSource.Token);
            //_immediateQueue.Enqueue(operation);
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
                    cancellationToken.Register(() =>
                    {
                        TaskCompletionSource.SetCanceled();
                    });
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
                    Scheduler.RunPreemptively(
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