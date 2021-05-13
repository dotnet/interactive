// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Server
{
    public class StdioKernelClient : KernelClientBase, IDisposable
    {
        private readonly ProcessStartInfo _processStartInfo;
        private readonly ISubject<KernelEvent> _kernelEvents = new Subject<KernelEvent>();
        private KernelClient _client;
        private readonly CompositeDisposable _disposables = new();

        public StdioKernelClient(ProcessStartInfo processStartInfo)
        {
            _processStartInfo = processStartInfo ?? throw new ArgumentNullException(nameof(processStartInfo));
        }

        public async Task StartAsync()
        {
            if (_client is null)
            {
                // start process

                var process = new Process { StartInfo = _processStartInfo };
                TaskCompletionSource<bool> ready = new();
                process.Start();

                var input = new TextReaderInputStream(process.StandardOutput);
                var output = new TextWriterOutputStream(process.StandardInput);
                var client = new KernelClient(input, output);

                _disposables.Add(client.KernelEvents.Subscribe(_kernelEvents));
                _disposables.Add(Disposable.Create(() =>
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                    _client?.Dispose();
                    _client = null;
                }));

                var sub = _kernelEvents.OfType<KernelReady>().Subscribe(_ =>
                {
                    ready.SetResult(true);
                });

               

                await ready.Task;
                _client = client;
                sub.Dispose();

            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        public override IObservable<KernelEvent> KernelEvents => _kernelEvents;
        public override Task SendAsync(KernelCommand command, string token = null)
        {
            if (_client is null)
            {
                throw new InvalidOperationException("The client has not started");
            }

            return _client.SendAsync(command, token);
        }
    }
}