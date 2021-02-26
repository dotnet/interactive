// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Microsoft.DotNet.Interactive
{
    internal sealed class KernelSynchronizationContext : SynchronizationContext, IDisposable
    {
        private bool _running = false;
        private readonly BlockingCollection<WorkItem> _queue = new();

        public KernelSynchronizationContext()
        {
            SetSynchronizationContext(this);
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

                RunUntilQueueIsEmpty();
            }
            catch (InvalidOperationException)
            {
                throw new ObjectDisposedException($"The {nameof(KernelSynchronizationContext)} has been disposed.");
            }
        }

        public override void Send(SendOrPostCallback callback, object state) =>
            throw new NotSupportedException($"Synchronous Send is not supported by {nameof(KernelSynchronizationContext)}.");

        public void Cancel()
        {
            Cancelled = true;
        }

        public bool Cancelled { get; private set; }

        private void RunUntilQueueIsEmpty()
        {
            if (_running)
            {
                return;
            }

            _running = true;

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