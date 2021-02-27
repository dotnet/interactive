// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive
{
    internal sealed class KernelSynchronizationContext : SynchronizationContext
    {
        private readonly CancellationToken _cancellationToken;
        private int _running = 0;
        private readonly BlockingCollection<WorkItem> _queue = new();

        public KernelSynchronizationContext(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
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
                EnsureWorkerIsRunning();

                _queue.Add(workItem, _cancellationToken);
            }
            catch (InvalidOperationException)
            {
                throw new ObjectDisposedException($"The {nameof(KernelSynchronizationContext)} has been disposed.");
            }
        }

        public override void Send(SendOrPostCallback callback, object state) =>
            throw new NotSupportedException($"Synchronous Send is not supported by {nameof(KernelSynchronizationContext)}.");

        private void EnsureWorkerIsRunning()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _running, 1, 0) == 1)
            {
                return;
            }

            Task.Run(() =>
            {
                foreach (var workItem in _queue.GetConsumingEnumerable(_cancellationToken))
                {
                    if (!_cancellationToken.IsCancellationRequested)
                    {
                        workItem.Run();
                    }
                }
            }, _cancellationToken);
        }

        public static void Run(Func<Task> func, CancellationToken cancellationToken = default)
        {
            var previousContext = Current;

            try
            {
                var syncCtx = new KernelSynchronizationContext(cancellationToken);

                SetSynchronizationContext(syncCtx);

                var t = func();

                t.ContinueWith(
                    _ => SetSynchronizationContext(previousContext),
                    TaskScheduler.Default);

                t.WaitAndUnwrapException();
            }
            catch (Exception)
            {
                SetSynchronizationContext(previousContext);
            }
        }

        private readonly struct WorkItem
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