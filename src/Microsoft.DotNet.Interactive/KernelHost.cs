// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive
{
    public class KernelHost : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new ();
        private readonly CompositeKernel _kernel;
        private readonly IKernelCommandAndEventSender _defaultSender;
        private readonly MultiplexingKernelCommandAndEventReceiver _defaultReceiver;
        private Task<Task> _runningLoop;
        public IKernelConnector DefaultConnector { get; }


        public KernelHost(CompositeKernel kernel,IKernelCommandAndEventSender defaultSender, MultiplexingKernelCommandAndEventReceiver defaultReceiver)
        {
            _kernel = kernel;
            _defaultSender = defaultSender;
            _defaultReceiver = defaultReceiver;
            DefaultConnector = new DefaultKernelConnector(_defaultSender, _defaultReceiver);
            _kernel.SetHost(this);
        }

        private class DefaultKernelConnector : IKernelConnector
        {
            private readonly IKernelCommandAndEventSender _defaultSender;
            private readonly MultiplexingKernelCommandAndEventReceiver _defaultReceiver;

            public DefaultKernelConnector(IKernelCommandAndEventSender defaultSender, MultiplexingKernelCommandAndEventReceiver defaultReceiver)
            {
                _defaultSender = defaultSender;
                _defaultReceiver = defaultReceiver;
            }

            public Task<Kernel> ConnectKernelAsync(KernelName kernelName)
            {
                var proxy = new ProxyKernel(kernelName.Name, _defaultReceiver.CreateChildReceiver(), _defaultSender);
                var _ = proxy.StartAsync();
                return Task.FromResult((Kernel)proxy);
            }
        }

        public async Task ConnectAsync()
        {
            if (_runningLoop is { })
            {
                throw new InvalidOperationException("The host is already connected.");
            }

            await _defaultSender.NotifyIsReadyAsync(_cancellationTokenSource.Token);

            _runningLoop = Task.Factory.StartNew(async () =>
            {
                await foreach (var commandOrEvent in _defaultReceiver.CommandsAndEventsAsync(_cancellationTokenSource.Token))
                {
                    if (commandOrEvent.IsParseError)
                    {
                        var _ = _defaultSender.SendAsync(commandOrEvent.Event, _cancellationTokenSource.Token);
                    }
                    else
                    {
                        await commandOrEvent.DispatchAsync(_kernel, _cancellationTokenSource.Token);
                    }
                }
            }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public async Task ConnectAndWaitAsync()
        {
            await ConnectAsync();
            await _runningLoop;
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }
}
