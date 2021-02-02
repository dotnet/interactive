// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Connection
{
    public sealed class ProxyKernel : Kernel
    {
        private readonly KernelClientBase _client;

        public ProxyKernel(string name, KernelClientBase client) : base(name)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));

            RegisterForDisposal(client.KernelEvents.Subscribe(OnKernelEvents));
        }

        private void OnKernelEvents(KernelEvent kernelEvent)
        {
            PublishEvent(kernelEvent);
        }

        internal override async Task HandleAsync(KernelCommand command, KernelInvocationContext context)
        {
            var targetKernelName = command.TargetKernelName;
            command.TargetKernelName = null;
            await _client.SendAsync(command);
            command.TargetKernelName = targetKernelName;
        }
    }
}