// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Tests.Utility;

public static class ConsoleLock
{
    // FIX: (ConsoleLock) clean up
    private static readonly AsyncLock _lock = new();

    public static async Task<IDisposable> AcquireAsync()
    {
        return await _lock.LockAsync();
    }
}

internal class AsyncLock
{
    private readonly AsyncSemaphore _semaphore;
    private readonly Task<Releaser> _releaser;

    public AsyncLock()
    {
        _semaphore = new AsyncSemaphore(1);
        _releaser = Task.FromResult(new Releaser(this));
    }

    public Task<Releaser> LockAsync()
    {
        var wait = _semaphore.WaitAsync();

        return wait.IsCompleted
                   ? _releaser
                   : wait.ContinueWith((_, state) => new Releaser((AsyncLock)state),
                                       this, CancellationToken.None,
                                       TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    public struct Releaser : IDisposable
    {
        private readonly AsyncLock _toRelease;

        internal Releaser(AsyncLock toRelease)
        {
            _toRelease = toRelease ?? throw new ArgumentNullException(nameof(toRelease));
        }

        public void Dispose() => _toRelease?._semaphore.Release();
    }
}

internal class AsyncSemaphore
{
    private static readonly Task _completed = Task.FromResult(true);
    private readonly Queue<TaskCompletionSource<bool>> _waiters = new();
    private int m_currentCount;

    public AsyncSemaphore(int initialCount)
    {
        if (initialCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCount));
        }

        m_currentCount = initialCount;
    }

    public Task WaitAsync()
    {
        lock (_waiters)
        {
            if (m_currentCount > 0)
            {
                --m_currentCount;
                return _completed;
            }
            else
            {
                var waiter = new TaskCompletionSource<bool>();
                _waiters.Enqueue(waiter);
                return waiter.Task;
            }
        }
    }

    public void Release()
    {
        TaskCompletionSource<bool> toRelease = null;

        lock (_waiters)
        {
            if (_waiters.Count > 0)
            {
                toRelease = _waiters.Dequeue();
            }
            else
            {
                ++m_currentCount;
            }
        }

        toRelease?.SetResult(true);
    }
}