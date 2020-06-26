// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive
{
    public sealed class ProxyKernel : Kernel, IKernelCommandHandler<SubmitCode>, IKernelCommandHandler<RequestCompletions>
    {
        private readonly KernelClient _client;

        public ProxyKernel(string name, KernelClient client) : base(name)
        {
            _client = client;
            RegisterForDisposal(client);
            RegisterForDisposal(() =>
            {
                client.KernelEvents.Subscribe(OnKernelEvents);
            });
        }

        private void OnKernelEvents(KernelEvent kernelEvent)
        {
            PublishEvent(kernelEvent);
        }

        public Task HandleAsync(SubmitCode command, KernelInvocationContext context)
        {
            return _client.SendAsync(command);
        }

        public Task HandleAsync(RequestCompletions command, KernelInvocationContext context)
        {
            return _client.SendAsync(command);
        }

    }
}