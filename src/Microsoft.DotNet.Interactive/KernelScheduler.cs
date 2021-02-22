// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public class KernelScheduler<T,U>
    {
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
                // get all deferred operations and pump in
                foreach (var deferredOperationRegistration in _deferredOperationRegistrations)
                {
                    foreach (var deferred in deferredOperationRegistration.GetDeferredOperations(operation.Value))
                    {
                        var deferredOperation = new ScheduledOperation(deferred, deferredOperationRegistration.OnExecute);
                        _executionScheduler.Schedule(() => DoWork(deferredOperation));
                    }
                }

                _executionScheduler.Schedule(() => DoWork(operation) );
            }

            _executionScheduler.Schedule(ProcessScheduledOperations);

            static void DoWork(ScheduledOperation operation)
            {
                try
                {
                    operation.OnExecuteAsync(operation.Value);
                    operation.CompletionSource.SetResult(default);
                }
                catch (Exception e)
                {
                    operation.CompletionSource.SetException(e);
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

            public TaskCompletionSource<U> CompletionSource { get;  }
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

        public void RegisterDeferredOperationSource(GetDeferredOperationsDelegate getDeferredOperations, OnExecuteDelegate onExecuteAsync)
        {
            _deferredOperationRegistrations.Add(new DeferredOperation(onExecuteAsync,getDeferredOperations));
        }
    }

   

    public static class KernelSchedulerExtensions
    {
        public static Task<U> Schedule<T,U>(this KernelScheduler<T,U> kernelScheduler, T value, Action<T> onExecute)
        {
            return kernelScheduler.Schedule(value, v =>
            {
                onExecute(v);
                return Task.CompletedTask;
            });
        }

        public static void RegisterDeferredOperationSource<T,U>(this KernelScheduler<T, U> kernelScheduler, KernelScheduler<T,U>.GetDeferredOperationsDelegate getDeferredOperations, Action<T> onExecute)
        {
            kernelScheduler.RegisterDeferredOperationSource(getDeferredOperations, v =>
            {
                onExecute(v);
                return Task.CompletedTask;
            });
        }
    }
}