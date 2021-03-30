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
        private static readonly Logger Log = new("KernelScheduler");
        private readonly List<DeferredOperationSource> _deferredOperationSources = new();
        private readonly CancellationTokenSource _schedulerDisposalSource = new();
        private readonly Task _runLoopTask;
        private readonly AsyncLocal<ScheduledOperation> _currentTopLevelOperation = new();

        private readonly BlockingCollection<ScheduledOperation> _topLevelScheduledOperations = new();
        private ScheduledOperation _currentlyRunningOperation;
        
        public KernelScheduler()
        {
            _runLoopTask = Task.Factory.StartNew(
                ScheduledOperationRunLoop,
                TaskCreationOptions.LongRunning,
                _schedulerDisposalSource.Token);
        }

        public void CancelCurrentOperation()
        {
            if (_currentlyRunningOperation is { } operation)
            {
                operation.TaskCompletionSource.TrySetCanceled(_schedulerDisposalSource.Token);
                _currentlyRunningOperation = null;
            }
        }

        public Task<U> RunAsync(
            T value,
            OnExecuteDelegate onExecuteAsync,
            string scope = "default",
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            ScheduledOperation operation;
            if (_currentTopLevelOperation.Value is { })
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
            else
            {
                operation = new ScheduledOperation(
                    value,
                    onExecuteAsync,
                    ExecutionContext.Capture(),
                    scope: scope,
                    cancellationToken: cancellationToken);
                _topLevelScheduledOperations.Add(operation, cancellationToken);
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

                _currentlyRunningOperation = operation;

                try
                {
                    ExecutionContext.Run(
                        executionContext,
                        _ => RunScheduledOperationAndDeferredOperations(operation),
                        operation);

                    operation.TaskCompletionSource.Task.Wait(_schedulerDisposalSource.Token);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                finally
                {
                    _currentTopLevelOperation.Value = null;
                    _currentlyRunningOperation = null;
                }
            }
        }

        private void Run(ScheduledOperation operation)
        {
            if (_currentTopLevelOperation.Value is null)
            {
                _currentTopLevelOperation.Value = operation;
            }

            using var logOp = Log.OnEnterAndConfirmOnExit($"Run: {operation.Value}");

            try
            {
                var operationTask = operation
                                    .ExecuteAsync()
                                    .ContinueWith(t =>
                                    {
                                        if (!operation.TaskCompletionSource.Task.IsCompleted)
                                        {
                                            if (t.IsCompletedSuccessfully)
                                            {
                                                operation.TaskCompletionSource.TrySetResult(t.Result);
                                            }
                                        }
                                    });

                Task.WaitAny(new[] {
                    operationTask,
                    operation.TaskCompletionSource.Task
                }, _schedulerDisposalSource.Token);

                logOp.Succeed();
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
            try
            {
                foreach (var deferredOperation in OperationsToRunBefore(operation))
                {
                    Run(deferredOperation);

                    if (!deferredOperation.TaskCompletionSource.Task.IsCompletedSuccessfully)
                    {
                        Log.Error(
                            "Deferred operation failed",
                            deferredOperation.TaskCompletionSource.Task.Exception);
                    }
                }

                Run(operation);
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }

        private void RunPreemptively(ScheduledOperation operation)
        {
            Run(operation);
            operation.TaskCompletionSource.Task.Wait(_schedulerDisposalSource.Token);
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

            _deferredOperationSources.Add(new DeferredOperationSource(onExecuteAsync, getDeferredOperations));
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

        public delegate IReadOnlyList<T> GetDeferredOperationsDelegate(T operationToExecute, string queueName);

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
                        TaskCompletionSource.TrySetCanceled();
                    });
                }
            }

            public TaskCompletionSource<U> TaskCompletionSource { get; }

            public T Value { get; }

            public ExecutionContext ExecutionContext { get; }

            public string Scope { get; }

            public Task<U> ExecuteAsync() => _onExecuteAsync(Value);

            public override string ToString()
            {
                return Value.ToString();
            }
        }

        private class DeferredOperationSource
        {
            public DeferredOperationSource(OnExecuteDelegate onExecuteAsync, GetDeferredOperationsDelegate getDeferredOperations)
            {
                OnExecuteAsync = onExecuteAsync;
                GetDeferredOperations = getDeferredOperations;
            }

            public GetDeferredOperationsDelegate GetDeferredOperations { get; }

            public OnExecuteDelegate OnExecuteAsync { get; }
        }
    }
}