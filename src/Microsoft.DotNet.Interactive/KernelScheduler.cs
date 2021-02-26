// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public class KernelScheduler<T, U> : IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource = new();

        private List<ScheduledOperation> _scheduledOperations = new();
        private List<DeferredOperation> _deferredOperationRegistrations = new();

        private readonly object _operationsLock = new();

        public Task<U> Schedule(T value, OnExecuteDelegate onExecuteAsync, string scope = "default")
        {
            var operation = new ScheduledOperation(value, onExecuteAsync, scope);

            lock (_operationsLock)
            {
                _cancellationTokenSource.Token.Register(() =>
                {
                    if (!operation.CompletionSource.Task.IsCompleted)
                    {
                        operation.CompletionSource.SetCanceled();
                    }
                });

                _scheduledOperations.Add(operation);

                if (_scheduledOperations.Count == 1)
                {
                    var previousSynchronizationContext = SynchronizationContext.Current;
                    var synchronizationContext = new KernelSynchronizationContext();

                    SynchronizationContext.SetSynchronizationContext(synchronizationContext);

                    Task.Run(async () =>
                    {
                        try
                        {
                            while (_scheduledOperations.Count > 0)
                            {
                                await ProcessScheduledOperations(_cancellationTokenSource.Token);
                            }
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }).ContinueWith(_ =>
                    {
                        SynchronizationContext.SetSynchronizationContext(previousSynchronizationContext);
                    });
                }
            }

            return operation.Task;
        }

        private async Task ProcessScheduledOperations(CancellationToken cancellationToken)
        {
            ScheduledOperation operation;

            lock (_operationsLock)
            {
                if (_scheduledOperations.Count > 0)
                {
                    operation = _scheduledOperations[0];
                    _scheduledOperations.RemoveAt(0);
                }
                else
                {
                    return;
                }
            }

            try
            {
                if (operation is not null)
                {
                    // get all deferred operations and pump in
                    foreach (var deferredOperationRegistration in _deferredOperationRegistrations)
                    {
                        foreach (var deferred in deferredOperationRegistration.GetDeferredOperations(operation.Value,
                            operation.Scope))
                        {
                            var deferredOperation = new ScheduledOperation(deferred,
                                deferredOperationRegistration.OnExecute, operation.Scope);

                            cancellationToken.Register(() =>
                            {
                                if (!deferredOperation.CompletionSource.Task.IsCompleted)
                                {
                                    deferredOperation.CompletionSource.SetCanceled();
                                }
                            });

                            await DoWork(deferredOperation);
                        }
                    }

                    await DoWork(operation);
                }
            }
            catch
            {
                Cancel();
                throw;
            }

            async Task DoWork(ScheduledOperation scheduleOperation)
            {
                if (!scheduleOperation.CompletionSource.Task.IsCanceled)
                {
                    try
                    {
                        var operationResult = await scheduleOperation.OnExecuteAsync(scheduleOperation.Value);
                        scheduleOperation.CompletionSource.SetResult(operationResult);
                    }
                    catch (Exception e)
                    {
                        scheduleOperation.CompletionSource.SetException(e);
                        throw;
                    }
                }
            }
        }

        public void RegisterDeferredOperationSource(GetDeferredOperationsDelegate getDeferredOperations, OnExecuteDelegate onExecuteAsync)
        {
            _deferredOperationRegistrations.Add(new DeferredOperation(onExecuteAsync, getDeferredOperations));
        }

        public void Cancel()
        {
            lock (_operationsLock)
            {


                if (SynchronizationContext.Current is KernelSynchronizationContext synchronizationContext)
                {
                    synchronizationContext.Cancel();
                }

                _scheduledOperations = new List<ScheduledOperation>();
                _deferredOperationRegistrations = new List<DeferredOperation>();

                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
            }
        }

        public void Dispose()
        {
            Cancel();
        }

        public delegate Task<U> OnExecuteDelegate(T value);

        public delegate IEnumerable<T> GetDeferredOperationsDelegate(T operationToExecute, string queueName);

        private class ScheduledOperation
        {
            public T Value { get; }
            public OnExecuteDelegate OnExecuteAsync { get; }
            public string Scope { get; }
            public Task<U> Task => CompletionSource.Task;


            public ScheduledOperation(T value, OnExecuteDelegate onExecuteAsync, string scope)
            {
                Value = value;
                CompletionSource = new TaskCompletionSource<U>();
                OnExecuteAsync = onExecuteAsync;
                Scope = scope;
            }

            public TaskCompletionSource<U> CompletionSource { get; }
        }

        private class DeferredOperation
        {
            public GetDeferredOperationsDelegate GetDeferredOperations { get; }
            public OnExecuteDelegate OnExecute { get; }
            public DeferredOperation(OnExecuteDelegate onExecute, GetDeferredOperationsDelegate getDeferredOperations)
            {
                OnExecute = onExecute;
                GetDeferredOperations = getDeferredOperations;
            }
        }

    }
}