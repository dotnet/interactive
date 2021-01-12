// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Connection
{
    /// <summary>
    /// Passes all commands on without any preconceived idea of whether it will be handled.
    /// </summary>
    /// <remarks>
    /// <see cref="ProxyKernel"/> only supports SubmitCode, RequestCompletions, RequestDiagnostics,
    /// and RequestHoverText. This forwards everything. It is used for commands explicitly routed to
    /// a client-side kernel running inside a notebook.
    /// </remarks>
    public class IndiscriminateProxyKernel : Kernel
    {
        private readonly KernelClientBase _client;

        public IndiscriminateProxyKernel(string name, KernelClientBase client) : base(name)
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