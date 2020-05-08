// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.App.Http
{
    public class KernelHubConnection : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private bool _registered;
        public CompositeKernel Kernel { get; }

        public KernelHubConnection(CompositeKernel kernel)
        {
            Kernel = kernel;
        }

        public void RegisterContext(IHubContext<KernelHub> hubContext)
        {
            if (!_registered)
            {
                _registered = true;
                _disposables.Add(Kernel.KernelEvents.Subscribe(onNext: async kernelEvent =>
                    await PublishEventToContext(kernelEvent, hubContext)));
            }
        }

        private async Task PublishEventToContext(IKernelEvent kernelEvent, IHubContext<KernelHub> hubContext)
        {
            var eventEnvelope = KernelEventEnvelope.Create(kernelEvent);

            await hubContext.Clients.All.SendAsync("kernelEvent", KernelEventEnvelope.Serialize(eventEnvelope));
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _registered = false;
        }
    }
}