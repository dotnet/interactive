// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Connection
{
    public sealed class ProxyKernel :
        Kernel,
        IKernelCommandHandler<SubmitCode>,
        IKernelCommandHandler<RequestCompletions>,
        IKernelCommandHandler<RequestHoverText>

    {
        private readonly KernelClient _client;

        public ProxyKernel(string name, KernelClient client) : base(name)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));

            RegisterForDisposal(client.KernelEvents.Subscribe(OnKernelEvents));
        }

        private void OnKernelEvents(KernelEvent kernelEvent)
        {
            PublishEvent(kernelEvent);
        }

        public Task HandleAsync(SubmitCode command, KernelInvocationContext context)
        {
            return SendCommandToRemoteKernel(command);
        }

        public Task HandleAsync(RequestCompletions command, KernelInvocationContext context)
        {
            return SendCommandToRemoteKernel(command);
        }

        public Task HandleAsync(RequestHoverText command, KernelInvocationContext context)
        {
            return SendCommandToRemoteKernel(command);
        }

        private async Task SendCommandToRemoteKernel(KernelCommand command)
        {
            var targetKernelName = command.TargetKernelName;
            command.TargetKernelName = null;
            await _client.SendAsync(command);
            command.TargetKernelName = targetKernelName;
        }
    }
}