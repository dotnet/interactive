// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
   
    public class KernelScheduler<T,U> : IDisposable
    {
        private CompositeDisposable _disposables = new();
        private readonly IScheduler _executionScheduler = TaskPoolScheduler.Default;

        private readonly List<ScheduledOperation> _scheduledOperations = new();
        private readonly List<DeferredOperation> _deferredOperationRegistrations = new();

        public Task<U> Schedule(T value, OnExecuteDelegate onExecuteAsync)
        {
            var operation = new ScheduledOperation(value, onExecuteAsync);

            lock (_scheduledOperations)

            {
                _scheduledOperations.Add(operation);
            }

            _executionScheduler.Schedule(ProcessScheduledOperations);
            

            return operation.Task;
        }

        private void ProcessScheduledOperations()
        {
            ScheduledOperation operation;
            lock (_scheduledOperations)
            {   if(_scheduledOperations.Count > 0)       
                {
                    operation = _scheduledOperations[0];
                    _scheduledOperations.RemoveAt(0);
                }
                else
                {
                    return;
                }
            }

            if(operation is not null)
            {
                var disposableStack = new DisposableStack();
                // get all deferred operations and pump in
                foreach (var deferredOperationRegistration in _deferredOperationRegistrations)
                {
                    foreach (var deferred in deferredOperationRegistration.GetDeferredOperations(operation.Value))
                    {
                        var deferredOperation = new ScheduledOperation(deferred, deferredOperationRegistration.OnExecute);
                        disposableStack.Push( _executionScheduler.Schedule(async () => await DoWork(deferredOperation)));
                    }
                }

                var disposableOperation = new CompositeDisposable
                {
                    Disposable.Create(() =>
                    {
                        operation.CompletionSource.TrySetCanceled();
                    }),
                    _executionScheduler.Schedule(async () => await DoWork(operation))
                };

                disposableStack.Push(disposableOperation);
                
                _disposables.Add(disposableStack);
            }

            _executionScheduler.Schedule(ProcessScheduledOperations);

            static async Task DoWork(ScheduledOperation operation)
            {
                if (!operation.CompletionSource.Task.IsCanceled)
                {
                    try
                    {
                        await operation.OnExecuteAsync(operation.Value);
                        operation.CompletionSource.SetResult(default);
                    }
                    catch (Exception e)
                    {
                        operation.CompletionSource.SetException(e);
                    }
                }
            }
        }

        public void RegisterDeferredOperationSource(GetDeferredOperationsDelegate getDeferredOperations, OnExecuteDelegate onExecuteAsync)
        {
            _deferredOperationRegistrations.Add(new DeferredOperation(onExecuteAsync,getDeferredOperations));
        }

        public void Cancel()
        {
            var disposables = _disposables;
            _disposables = new CompositeDisposable();
            disposables.Dispose();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        private class DisposableStack : Stack<IDisposable>, IDisposable
        {
            public void Dispose()
            {
                while (Count > 0)
                {
                    try
                    {
                        Pop().Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
        }

        public delegate Task OnExecuteDelegate(T value);

        public delegate IEnumerable<T> GetDeferredOperationsDelegate(T operationToExecute);

        private class ScheduledOperation
        {
            public T Value { get; }
            public OnExecuteDelegate OnExecuteAsync { get; }
            public Task<U> Task => CompletionSource.Task;


            public ScheduledOperation(T value, OnExecuteDelegate onExecuteAsync)
            {
                Value = value;
                CompletionSource = new TaskCompletionSource<U>();
                OnExecuteAsync = onExecuteAsync;
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