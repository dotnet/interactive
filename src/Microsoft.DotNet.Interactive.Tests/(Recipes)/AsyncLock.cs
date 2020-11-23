using System;
using System.Threading;
using System.Threading.Tasks;
using Pocket;

namespace Recipes
{
    internal class AsyncLock
    {
        private readonly AsyncSemaphore _semaphore;

        public AsyncLock()
        {
          //  _semaphore = new AsyncSemaphore(1);
        }

        public Task<IDisposable> LockAsync()
        {
            // await _semaphore.WaitAsync();

            return Task.FromResult(Disposable.Create(() =>
            {
                // _semaphore.Release();
            }));
        }
    }
}
