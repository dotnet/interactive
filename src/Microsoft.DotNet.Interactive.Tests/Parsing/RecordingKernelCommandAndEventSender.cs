// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Tests.Parsing
{
    internal class RecordingKernelCommandAndEventSender : IKernelCommandAndEventSender
    {
        private Func<CommandOrEvent, Task> _onSendAsync;
        public List<KernelCommand> Commands { get; } = new();
        public List<KernelEvent> Events { get; } = new();
        public Task SendAsync(KernelCommand kernelCommand, CancellationToken cancellationToken)
        {
            Commands.Add(kernelCommand);
            _onSendAsync?.Invoke(new CommandOrEvent(KernelCommandEnvelope.Deserialize(KernelCommandEnvelope.Serialize(kernelCommand)).Command));
            return Task.CompletedTask;
        }

        public Task SendAsync(KernelEvent kernelEvent, CancellationToken cancellationToken)
        {
            Events.Add(kernelEvent);
            _onSendAsync?.Invoke(new CommandOrEvent(KernelEventEnvelope.Deserialize(KernelEventEnvelope.Serialize(kernelEvent)).Event));
            return Task.CompletedTask;
        }

        public void OnSend(Action<CommandOrEvent> onSend)
        {
            _onSendAsync = (commandOrEvent) =>
            {
                
                onSend(commandOrEvent);
                return Task.CompletedTask;
            };
        }

        public void OnSend(Func<CommandOrEvent, Task> onSendAsync)
        {
            _onSendAsync = onSendAsync;
        }
    }
}