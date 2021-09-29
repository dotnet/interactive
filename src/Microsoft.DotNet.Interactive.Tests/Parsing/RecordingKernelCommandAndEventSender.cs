// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Tests.Parsing
{
    internal class RecordingKernelCommandAndEventSender : IKernelCommandAndEventSender
    {
        private Action<CommandOrEvent> _onSend;
        public List<KernelCommand> Commands { get; } = new();
        public List<KernelEvent> Events { get; } = new();
        public Task SendAsync(KernelCommand kernelCommand, CancellationToken cancellationToken)
        {
            Commands.Add(kernelCommand);
            _onSend?.Invoke(new CommandOrEvent(kernelCommand));
            return Task.CompletedTask;
        }

        public Task SendAsync(KernelEvent kernelEvent, CancellationToken cancellationToken)
        {
            Events.Add(kernelEvent);
            _onSend?.Invoke(new CommandOrEvent(kernelEvent));
            return Task.CompletedTask;
        }

        public void OnSend(Action<CommandOrEvent> onSend)
        {
            _onSend = onSend;
        }
    }
}