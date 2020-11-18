// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.DotNet.Interactive.Messages;

namespace Microsoft.DotNet.Interactive.Http
{
    public class KernelHubConnection : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private bool _registered;
        public Kernel Kernel { get; }

        public KernelHubConnection(Kernel kernel)
        {
            Kernel = kernel;
        }

        public void RegisterContext(IHubContext<KernelHub> hubContext)
        {
            if (!_registered)
            {
                _registered = true;
                _disposables.Add(Kernel.KernelMessages.Subscribe(onNext: async kernelMessage =>
                    await PublishMessageToContext(kernelMessage, hubContext)));
            }
        }

        private async Task PublishMessageToContext(KernelChannelMessage kernelMessage, IHubContext<KernelHub> hubContext)
        {
            object data = KernelChannelMessage.SerializeToModel(kernelMessage);

            await hubContext.Clients.All.SendAsync("messages", data);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _registered = false;
        }
    }
}