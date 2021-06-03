// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Server
{
    public class KernelServer : IDisposable
    {
        private readonly Kernel _kernel;
        private readonly IKernelCommandAndEventReceiver _receiver;
        private readonly IKernelCommandAndEventSender _sender;
        private readonly CompositeDisposable _disposables = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public KernelServer(Kernel kernel,
            IKernelCommandAndEventReceiver receiver,
            IKernelCommandAndEventSender sender,
            DirectoryInfo workingDir)
        {
            if (workingDir is null)
            {
                throw new ArgumentNullException(nameof(workingDir));
            }

            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            Environment.CurrentDirectory = workingDir.FullName;

            _disposables.Add(_kernel.KernelEvents.Subscribe(async kernelEvent =>
            {
                if (kernelEvent is ReturnValueProduced { Value: DisplayedValue })
                {
                    return;
                }

                await _sender.SendAsync(kernelEvent, _cancellationTokenSource.Token);
            }));


            _disposables.Add(Disposable.Create(() =>
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }));
        }

        public void NotifyIsReady()
        {
            _sender.SendAsync(new KernelReady(), _cancellationTokenSource.Token)
                .Wait(_cancellationTokenSource.Token);
        }

        public Task RunAsync()
        {
            return Task.Run(async () =>
            {
                await foreach (var commandOrEvent in _receiver.CommandsOrEventsAsync(_cancellationTokenSource.Token))
                {
                    if (commandOrEvent.IsParseError)
                    {
                        var _ = _sender.SendAsync(commandOrEvent.Event, _cancellationTokenSource.Token);
                    }
                    else if (commandOrEvent.Command is { })
                    {
                        var _ = _kernel.SendAsync(commandOrEvent.Command, _cancellationTokenSource.Token);
                    }
                }
            }, _cancellationTokenSource.Token);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }

}
