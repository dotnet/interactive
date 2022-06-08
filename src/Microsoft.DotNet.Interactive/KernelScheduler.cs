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
    public class KernelScheduler<T, TResult> : IDisposable, IKernelScheduler<T, TResult>
    {
        private static readonly Logger Log = new("KernelScheduler");

        private readonly CompositeDisposable _disposables;
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

            _disposables = new CompositeDisposable
            {
                _topLevelScheduledOperations,
                () => { _schedulerDisposalSource.Cancel(); }
            };
        }

        public void CancelCurrentOperation(Action<T> onCancellation = null)
        {
            if (_currentlyRunningOperation is { } operation)
            {
                onCancellation?.Invoke(operation.Value);
                operation.TaskCompletionSource.TrySetCanceled(_schedulerDisposalSource.Token);
                _currentlyRunningOperation = null;
            }
        }

        public Task<TResult> RunAsync(
            T value,
            KernelSchedulerDelegate<T, TResult> onExecuteAsync,
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
                    false,
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
                    false,
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
                _currentTopLevelOperation.Value = operation;

                var executionContext = operation.ExecutionContext;

                if (executionContext is null)
                {
                    Log.Warning($"{nameof(operation.ExecutionContext)} was null for operation {operation}");
                    executionContext = ExecutionContext.Capture();
                }

                _currentlyRunningOperation = operation;

                try
                {
                    ExecutionContext.Run(
                        executionContext!,
                        _ => RunScheduledOperationAndDeferredOperations(operation),
                        operation);

                    operation.TaskCompletionSource.Task.Wait(_schedulerDisposalSource.Token);
                }
                catch (Exception e)
                {
                    Log.Error("while executing {operation}", e, operation);
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
            _currentTopLevelOperation.Value ??= operation;

            using var logOp = Log.OnEnterAndConfirmOnExit($"Run: {operation.Value}");

            try
            {
                var operationTask = operation
                                    .ExecuteAsync()
                                    .ContinueWith(t =>
                                    {
                                        if (!operation.TaskCompletionSource.Task.IsCompleted)
                                        {
                                            if (t.GetIsCompletedSuccessfully())
                                            {
                                                operation.TaskCompletionSource.TrySetResult(t.Result);
                                            }
                                        }
                                    });

                Task.WaitAny(new[]
                {
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
            for (var i = 0; i < _deferredOperationSources.Count; i++)
            {
                var source = _deferredOperationSources[i];

                var deferredOperations = source.GetDeferredOperations(
                    operation.Value,
                    operation.Scope);

                for (var j = 0; j < deferredOperations.Count; j++)
                {
                    var deferred = deferredOperations[j];

                    var deferredOperation = new ScheduledOperation(
                        deferred,
                        source.OnExecuteAsync,
                        true,
                        scope: operation.Scope);

                    yield return deferredOperation;
                }
            }
        }

        public void RegisterDeferredOperationSource(
            GetDeferredOperationsDelegate getDeferredOperations,
            KernelSchedulerDelegate<T, TResult> kernelSchedulerOnExecuteAsync)
        {
            ThrowIfDisposed();

            _deferredOperationSources.Add(new DeferredOperationSource(kernelSchedulerOnExecuteAsync, getDeferredOperations));
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_schedulerDisposalSource.IsCancellationRequested)
            {
                throw new ObjectDisposedException($"{nameof(KernelScheduler<T, TResult>)} has been disposed.");
            }
        }

        public delegate IReadOnlyList<T> GetDeferredOperationsDelegate(T operationToExecute, string queueName);

        private class ScheduledOperation
        {
            private readonly KernelSchedulerDelegate<T, TResult> _onExecuteAsync;

            public ScheduledOperation(
                T value,
                KernelSchedulerDelegate<T, TResult> onExecuteAsync,
                bool isDeferred,
                ExecutionContext executionContext = default,
                string scope = "default",
                CancellationToken cancellationToken = default)
            {
                Value = value;
                IsDeferred = isDeferred;
                ExecutionContext = executionContext;
                _onExecuteAsync = onExecuteAsync;
                Scope = scope;

                TaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

                if (cancellationToken != default)
                {
                    cancellationToken.Register(() =>
                    {
                        TaskCompletionSource.TrySetCanceled();
                    });
                }
            }

            public TaskCompletionSource<TResult> TaskCompletionSource { get; }

            public T Value { get; }

            public bool IsDeferred { get; }

            public ExecutionContext ExecutionContext { get; }

            public string Scope { get; }

            public Task<TResult> ExecuteAsync() => _onExecuteAsync(Value);

            public override string ToString()
            {
                return Value.ToString();
            }
        }

        private class DeferredOperationSource
        {
            public DeferredOperationSource(KernelSchedulerDelegate<T, TResult> kernelSchedulerOnExecuteAsync, GetDeferredOperationsDelegate getDeferredOperations)
            {
                OnExecuteAsync = kernelSchedulerOnExecuteAsync;
                GetDeferredOperations = getDeferredOperations;
            }

            public GetDeferredOperationsDelegate GetDeferredOperations { get; }

            public KernelSchedulerDelegate<T, TResult> OnExecuteAsync { get; }
        }
    }

    static class DotNetStandardHelpers
    {
#if !NETSTANDARD2_0
        internal static bool GetIsCompletedSuccessfully(this Task task)
        {
            return task.IsCompletedSuccessfully;
        }
#else
        // NetStandard 2.1
        // internal const int TASK_STATE_RAN_TO_COMPLETION = 0x1000000;                          // bin: 0000 0001 0000 0000 0000 0000 0000 0000
        // public bool IsCompletedSuccessfully => (m_stateFlags & TASK_STATE_COMPLETED_MASK) == TASK_STATE_RAN_TO_COMPLETION;
        // <see cref="IsCompleted"/> will return true when the Task is in one of the three
        // final states: <see cref="System.Threading.Tasks.TaskStatus.RanToCompletion">RanToCompletion</see>,
        // <see cref="System.Threading.Tasks.TaskStatus.Faulted">Faulted</see>, or
        // <see cref="System.Threading.Tasks.TaskStatus.Canceled">Canceled</see>.
        static public bool GetIsCompletedSuccessfully(this Task task)
        {
            return task.IsCompleted && !task.IsFaulted && !task.IsCanceled;
        }
#endif
    }
}