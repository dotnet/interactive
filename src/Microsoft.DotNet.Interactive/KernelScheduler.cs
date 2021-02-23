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
        private CancellationTokenSource _cancellationTokenSource = new();
        private static readonly Logger Logger = new Logger("Scheduler");

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
                    if(!operation.CompletionSource.Task.IsCompleted)
                    {
                        operation.CompletionSource.SetCanceled();
                    }
                });

                _scheduledOperations.Add(operation);
                if (_scheduledOperations.Count == 1)
                {
                    var previousSynchronizationContext = SynchronizationContext.Current;
                    var synchronizationContext = new ClockwiseSynchronizationContext();
                    
                    SynchronizationContext.SetSynchronizationContext(synchronizationContext);
                    Task.Run(async () =>
                    {
                        while (_scheduledOperations.Count > 0)
                        {
                            await ProcessScheduledOperations(_cancellationTokenSource.Token);
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
            using var _ = Logger.OnEnterAndExit();
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

            if (operation is not null)
            {
                // get all deferred operations and pump in
                foreach (var deferredOperationRegistration in _deferredOperationRegistrations)
                {
                    foreach (var deferred in deferredOperationRegistration.GetDeferredOperations(operation.Value, operation.Scope))
                    {
                        var deferredOperation = new ScheduledOperation(deferred, deferredOperationRegistration.OnExecute, operation.Scope);
                        
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



            async Task DoWork(ScheduledOperation scheduleOperation)
            {
                using var _ = Logger.OnEnterAndExit("DoWork");
                if (!scheduleOperation.CompletionSource.Task.IsCanceled)
                {
                    try
                    {
                        await scheduleOperation.OnExecuteAsync(scheduleOperation.Value);
                        scheduleOperation.CompletionSource.SetResult(default);
                    }
                    catch (Exception e)
                    {
                        scheduleOperation.CompletionSource.SetException(e);
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


                if (SynchronizationContext.Current is ClockwiseSynchronizationContext synchronizationContext)
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



        public delegate Task OnExecuteDelegate(T value);

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

    internal sealed class ClockwiseSynchronizationContext : SynchronizationContext, IDisposable
    {
        private static readonly Logger Logger = new Logger("SynchronizationContext");

        private readonly BlockingCollection<WorkItem> _queue = new();

        public ClockwiseSynchronizationContext()
        {
            var thread = new Thread(Run);

            thread.Start();
        }

        public override void Post(SendOrPostCallback callback, object state)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            var workItem = new WorkItem(callback, state);

            try
            {
                _queue.Add(workItem);
            }
            catch (InvalidOperationException)
            {
                throw new ObjectDisposedException($"The {nameof(ClockwiseSynchronizationContext)} has been disposed.");
            }
        }

        public override void Send(SendOrPostCallback callback, object state) =>
            throw new NotSupportedException($"Synchronous Send is not supported by {nameof(ClockwiseSynchronizationContext)}.");

        public void Cancel()
        {
            Cancelled = true;
        }

        public bool Cancelled { get; private set; }

        private void Run()
        {
            SetSynchronizationContext(this);

            foreach (var workItem in _queue.GetConsumingEnumerable())
            {
                if (!Cancelled)
                {
                    workItem.Run();
                }

            }
        }

        public void Dispose() => _queue.CompleteAdding();

        private struct WorkItem
        {
            public WorkItem(SendOrPostCallback callback, object state)
            {
                Callback = callback;
                State = state;
            }

            private readonly SendOrPostCallback Callback;

            private readonly object State;

            public void Run() => Callback(State);
        }
    }
}