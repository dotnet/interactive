using System;
using System.Threading;
using System.Threading.Tasks;

namespace Recipes
{
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
                       : wait.ContinueWith((_, state) => new Releaser((AsyncLock) state),
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
}
