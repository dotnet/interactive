// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
        private FrontEndKernel _frontEndKernel;

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
                var frontEndKernelNames = new HashSet<string>();
                if (_frontEndKernel != null)
                {
                    frontEndKernelNames.Add(_frontEndKernel.Name);
                }

                // if it came from front end, bail out
                if (kernelEvent.Command.TargetKernelName is not null &&
                    frontEndKernelNames.Contains(kernelEvent.Command.TargetKernelName))
                {
                    return;
                }

                if (kernelEvent is ReturnValueProduced { Value: DisplayedValue })
                {
                   return;
                }

                try
                {
                    await SendAsync(kernelEvent, _cancellationTokenSource.Token);
                }
                catch
                {

                }
            }));


            _disposables.Add(Disposable.Create(() =>
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }));
        }

        public void NotifyIsReady()
        {
            SendAsync(new KernelReady(), _cancellationTokenSource.Token)
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
                        var _ = SendAsync(commandOrEvent.Event, _cancellationTokenSource.Token);
                    }
                    else if (commandOrEvent.Command is { })
                    {
                        if (_frontEndKernel?.ExecutionContext is not null)
                        {
                            ExecutionContext.Run(_frontEndKernel.ExecutionContext, (c) =>
                            {
                                var _ = _kernel.SendAsync(commandOrEvent.Command, _cancellationTokenSource.Token);
                            }, null);
                        }                        
                        else
                        {
                            var _ = _kernel.SendAsync(commandOrEvent.Command, _cancellationTokenSource.Token);
                        }
                    }
                    else if (commandOrEvent.Event is { })
                    {
                        _frontEndKernel?.ForwardEvent(commandOrEvent.Event);
                    }
                }
            }, _cancellationTokenSource.Token);
        }

        public Kernel GetFrontEndKernel(string kernelName)
        {
            if (_frontEndKernel == null)
            {
                _frontEndKernel = new FrontEndKernel(kernelName, _sender);
            }

            return _frontEndKernel;
        }

        private Task SendAsync(KernelEvent @event, CancellationToken cancellationToken)
        {
            return _sender.SendAsync(@event, cancellationToken);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
