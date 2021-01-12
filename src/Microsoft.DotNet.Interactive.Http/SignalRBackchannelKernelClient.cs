// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Http
{
    public class SignalRBackchannelKernelClient : KernelClientBase
    {
        private IHubContext<KernelHub> _hubContext;
        private readonly Subject<KernelEvent> _kernelEventsFromClient = new Subject<KernelEvent>();

        public override IObservable<KernelEvent> KernelEvents => _kernelEventsFromClient;

        public override async Task SendAsync(KernelCommand command, string token = null)
        {
            string commandEnvelope = KernelCommandEnvelope.Serialize(command);
            await _hubContext.Clients.All.SendAsync("submitCommand", commandEnvelope);
        }

        internal void SetContext(IHubContext<KernelHub> hubContext)
        {
            _hubContext = hubContext;
        }

        internal Task HandleKernelEventFromClientAsync(IKernelEventEnvelope envelope)
        {
            _kernelEventsFromClient.OnNext(envelope.Event);
            return Task.CompletedTask;
        }
    }
}