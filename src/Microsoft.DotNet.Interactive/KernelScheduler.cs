// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public class KernelScheduler<T, U> : IDisposable
    {
        private readonly List<DeferredOperation> _deferredOperationSources = new();
        private readonly ConcurrentQueue<ScheduledOperation> _queue = new();
        private readonly Task _loop;
        private readonly CancellationTokenSource _schedulerDisposalSource = new();
        private KernelSynchronizationContext _synchronizationContext;
        private readonly ManualResetEventSlim _mre = new(false);
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

            var operation = new ScheduledOperation(value, onExecuteAsync, scope, cancellationToken);

            _queue.Enqueue(operation);

            lock (_lockObj)
            {
                _mre.Set();
            }

            return operation.TaskCompletionSource.Task;
        }

        private void RunScheduledOperations(object _)
        {
            var cancellationToken = _schedulerDisposalSource.Token;

            // var previousContext = SynchronizationContext.Current;
            //
            // _synchronizationContext = new KernelSynchronizationContext(cancellationToken);
            //
            // SynchronizationContext.SetSynchronizationContext(_synchronizationContext);

            try
            {
                while (!_schedulerDisposalSource.IsCancellationRequested)
                {
                    _mre.Wait(_schedulerDisposalSource.Token);

                    while (!_schedulerDisposalSource.IsCancellationRequested && 
                           _queue.TryDequeue(out var operation))
                    {
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

                            operationTask.Wait(cancellationToken);
                        }
                        catch (Exception exception)
                        {
                            operation.TaskCompletionSource.SetException(exception);

                            _queue.Clear();
                        }
                    }
                }
            }
            finally
            {
                // SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }

        private void ProcessDeferredOperation(ScheduledOperation operation)
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
                        source.OnExecuteAsync, operation.Scope);

                    // DoWork(deferredOperation, cancellationToken);
                }
            }

            // DoWork(operation, cancellationToken);
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
                string scope,
                CancellationToken cancellationToken = default)
            {
                Value = value;
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
    }
}