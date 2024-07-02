// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pocket;

namespace Microsoft.DotNet.Interactive;

public class KernelScheduler<T, TResult> : IDisposable, IKernelScheduler<T, TResult>
{
    private static readonly Logger Log = new("KernelScheduler");

    private readonly CompositeDisposable _disposables;
    private readonly List<DeferredOperationSource> _deferredOperationSources = new();
    private readonly CancellationTokenSource _schedulerDisposalSource = new();
    private readonly Task _runLoopTask;

    private readonly BlockingCollection<ScheduledOperation> _topLevelScheduledOperations = new();
    private ScheduledOperation _currentlyRunningTopLevelOperation;
    private ScheduledOperation _currentlyRunningOperation;
    private readonly Barrier _childOperationsBarrier = new(1);

    public KernelScheduler()
    {
        _runLoopTask = Task.Factory.StartNew(
           ScheduledOperationRunLoop,
           creationOptions: TaskCreationOptions.LongRunning);

        _disposables = new CompositeDisposable
        {
            _schedulerDisposalSource.Cancel,
            _schedulerDisposalSource,
            _topLevelScheduledOperations,
        };
    }

    public void CancelCurrentOperation()
    {
        if (_currentlyRunningTopLevelOperation is { } operation)
        {
            _currentlyRunningTopLevelOperation = null;
            _currentlyRunningOperation = null;
            operation.TaskCompletionSource.TrySetCanceled(_schedulerDisposalSource.Token);
        }
    }

    public T CurrentValue =>
        (_currentlyRunningOperation ?? _currentlyRunningTopLevelOperation) is { } currentOperation
            ? currentOperation.Value
            : default;

    public Task<TResult> RunAsync(
        T value,
        KernelSchedulerDelegate<T, TResult> onExecuteAsync,
        string scope = "default",
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        ScheduledOperation operation;

        if (_currentlyRunningTopLevelOperation is { } currentlyRunningOperation &&
            IsChildOperation(currentlyRunningOperation.Value, value))
        {
            operation = new ScheduledOperation(
                value,
                onExecuteAsync,
                isDeferred: false,
                currentlyRunningOperation,
                executionContext: null,
                scope,
                cancellationToken);
            currentlyRunningOperation.AddChild(operation);
            RunChildOperation(operation);
        }
        else
        {
            operation = new ScheduledOperation(
                value,
                onExecuteAsync,
                isDeferred: false,
                parentOperation: null,
                ExecutionContext.Capture(),
                scope: scope,
                cancellationToken: cancellationToken);
            EnqueueTopLevelOperation(operation);
        }

        return operation.TaskCompletionSource.Task;
    }

    internal async Task IdleAsync()
    {
        if (_currentlyRunningTopLevelOperation is { IsCompleted: false } currentlyRunning)
        {
            await currentlyRunning.TaskCompletionSource.Task;
        }

        _childOperationsBarrier.SignalAndWait();
    }

    private void ScheduledOperationRunLoop()
    {
        try
        {
            foreach (var operation in _topLevelScheduledOperations.GetConsumingEnumerable(_schedulerDisposalSource.Token))
            {
                _currentlyRunningTopLevelOperation = operation;

                var executionContext = operation.ExecutionContext;

                try
                {
                    ExecutionContext.Run(
                        executionContext!.CreateCopy(),
                        _ => RunDeferredOperationsAndThen(operation),
                        operation);

                    operation.TaskCompletionSource.Task.Wait(_schedulerDisposalSource.Token);
                }
                catch (Exception e)
                {
                    Log.Error("while executing {operation}", e, operation);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Run(ScheduledOperation operation)
    {
        using var logOp = Log.OnEnterAndConfirmOnExit(arg: operation.Value);

        try
        {
            _currentlyRunningOperation = operation;

            var operationTask = operation
                                .ExecuteAsync()
                                .ContinueWith(t =>
                                {
                                    if (!operation.IsCompleted)
                                    {
                                        if (t.GetIsCompletedSuccessfully())
                                        {
                                            CompleteWithResult(t.Result);
                                        }
                                        else if (t.Exception is { })
                                        {
                                            CompleteWithException(t.Exception);
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
            if (!operation.IsCompleted)
            {
                CompleteWithException(exception);
            }
        }

        void CompleteWithResult(TResult result)
        {
            ResetCurrentlyRunning();
            operation.TaskCompletionSource.TrySetResult(result);
        }

        void CompleteWithException(Exception exception)
        {
            ResetCurrentlyRunning();
            operation.TaskCompletionSource.SetException(exception);
        }

        void ResetCurrentlyRunning()
        {
            if (ReferenceEquals(_currentlyRunningTopLevelOperation, operation))
            {
                _currentlyRunningTopLevelOperation = null;
            }

            _currentlyRunningOperation = null;
        }
    }

    private void EnqueueTopLevelOperation(ScheduledOperation operation)
    {
        _topLevelScheduledOperations.Add(operation, operation.CancellationToken);
    }

    private void RunChildOperation(ScheduledOperation operation)
    {
        try
        {
            _childOperationsBarrier.AddParticipant();

            RunDeferredOperationsAndThen(operation);
        }
        finally
        {
            _childOperationsBarrier.RemoveParticipant();
        }
    }

    private void RunDeferredOperationsAndThen(ScheduledOperation operation)
    {
        try
        {
            foreach (ScheduledOperation deferredOperation in GetDeferredOperationsToRunBefore(operation).ToArray())
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

    private IEnumerable<ScheduledOperation> GetDeferredOperationsToRunBefore(ScheduledOperation operation)
    {
        List<ScheduledOperation> scheduledOperations = null;

        for (var i = 0; i < _deferredOperationSources.Count; i++)
        {
            var source = _deferredOperationSources[i];

            var deferredOperations = source.GetDeferredOperations(
                operation.Value,
                operation.Scope);

            deferredOperations.ContinueWith(task =>
            {
                scheduledOperations ??= new();

                for (var j = 0; j < task.Result.Count; j++)
                {
                    var deferred = task.Result[j];

                    var scheduledOperation = new ScheduledOperation(
                        deferred,
                        source.OnExecuteAsync,
                        true,
                        parentOperation: null,
                        scope: operation.Scope);

                    scheduledOperations.Add(scheduledOperation);
                }
            }).Wait();
        }

        return (IEnumerable<ScheduledOperation>)scheduledOperations ?? Array.Empty<ScheduledOperation>();
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

    public delegate Task<IReadOnlyList<T>> GetDeferredOperationsDelegate(T operationToExecute, string queueName);

    private class ScheduledOperation
    {
        private static readonly Action<Action, CancellationToken> _runWithControlledExecution = default;

        static ScheduledOperation()
        {
            try
            {
                // todo: this is still a problem with fsi

                // ControlledExecution.Run isn't available in .NET Standard but since we're most likely actually running in .NET 7+, we can try to bind a delegate to it.

                //if (Type.GetType("System.Runtime.ControlledExecution, System.Private.CoreLib", false) is { } controlledExecutionType &&
                //    controlledExecutionType.GetMethod("Run", BindingFlags.Static | BindingFlags.Public) is { } runMethod)
                //{
                //    var actionParameter = Expression.Parameter(typeof(Action), "action");

                //    var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

                //    _runWithControlledExecution = Expression.Lambda<Action<Action, CancellationToken>>(
                //                                                Expression.Call(runMethod, actionParameter, cancellationTokenParameter),
                //                                                actionParameter,
                //                                                cancellationTokenParameter)
                //                                            .Compile();
                //}
            }
            catch
            {
            }
        }

        private readonly KernelSchedulerDelegate<T, TResult> _onExecuteAsync;

        private readonly List<ScheduledOperation> _childOperations = new();

        public ScheduledOperation(
            T value,
            KernelSchedulerDelegate<T, TResult> onExecuteAsync,
            bool isDeferred,
            ScheduledOperation parentOperation = null,
            ExecutionContext executionContext = default,
            string scope = "default",
            CancellationToken cancellationToken = default)
        {
            Value = value;
            IsDeferred = isDeferred;

            ExecutionContext = executionContext;
            _onExecuteAsync = onExecuteAsync;
            CancellationToken = cancellationToken;
            Scope = scope;

            TaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            if (parentOperation is not null)
            {
                IsChildOperation = true;
                var parentOperationCancellationToken = parentOperation.CancellationToken;
                if (parentOperationCancellationToken.CanBeCanceled)
                {
                    parentOperationCancellationToken.Register(() =>
                    {
                        TaskCompletionSource.TrySetCanceled();
                    });
                }
            }

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() => { TaskCompletionSource.TrySetCanceled(); });
            }
        }

        public readonly CancellationToken CancellationToken;

        public TaskCompletionSource<TResult> TaskCompletionSource { get; }

        public T Value { get; }

        public bool IsCompleted => TaskCompletionSource.Task.IsCompleted;

        public bool IsDeferred { get; }

        public bool IsChildOperation { get; }

        public ExecutionContext ExecutionContext { get; }

        public string Scope { get; }

        public Task<TResult> ExecuteAsync()
        {
            if (_runWithControlledExecution is not null &&
                CancellationToken.CanBeCanceled)
            {
                try
                {
                    TResult result = default;

                    _runWithControlledExecution(() =>
                    {
                        var r = _onExecuteAsync(Value).GetAwaiter().GetResult();
                        result = r;

                    }, CancellationToken);

                    return Task.FromResult(result);
                }
                catch (Exception exception)
                {
                    return Task.FromException<TResult>(exception);
                }
            }
            else
            {
                var result = _onExecuteAsync(Value);

                return result;
            }
        }

        public override string ToString() => Value.ToString();

        public void AddChild(ScheduledOperation operation)
        {
            lock (_childOperations)
            {
                _childOperations.Add(operation);
            }
        }

        public IEnumerable GetChildOperations()
        {
            ScheduledOperation[] operations;
            lock (_childOperations)
            {
                operations = _childOperations.ToArray();
            }
            return operations;
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

    protected virtual bool IsChildOperation(T current, T incoming) => false;
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