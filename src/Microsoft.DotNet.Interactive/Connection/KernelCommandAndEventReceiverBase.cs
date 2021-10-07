// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Connection
{
    public abstract class KernelCommandAndEventReceiverBase : IKernelCommandAndEventReceiver
    {
        protected abstract Task<CommandOrEvent> ReadCommandOrEventAsync(CancellationToken cancellationToken);

        public async IAsyncEnumerable<CommandOrEvent> CommandsAndEventsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {

                var commandOrEvent = await ReadCommandOrEventAsync(cancellationToken);

                if (commandOrEvent is null)
                {
                    continue;
                }
               
                yield return commandOrEvent;
            }
        }
    }

    public abstract class InteractiveProtocolKernelCommandAndEventReceiverBase : KernelCommandAndEventReceiverBase
    {
        protected abstract Task<string> ReadMessageAsync(CancellationToken cancellationToken);

        protected override async Task<CommandOrEvent> ReadCommandOrEventAsync(CancellationToken cancellationToken)
        {
            KernelCommand kernelCommand = null;
            KernelEvent kernelEvent = null;

            var message = await ReadMessageAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            var isParseError = false;
            try
            {
                var jsonObject = JsonDocument.Parse(message).RootElement;
                if (IsEventEnvelope(jsonObject))
                {
                    var kernelEventEnvelope = KernelEventEnvelope.Deserialize(jsonObject);
                    kernelEvent = kernelEventEnvelope.Event;
                }
                else if (IsCommandEnvelope(jsonObject))
                {
                    var kernelCommandEnvelope = KernelCommandEnvelope.Deserialize(jsonObject);
                    kernelCommand = kernelCommandEnvelope.Command;
                }
                else
                {
                    kernelEvent = new DiagnosticLogEntryProduced(
                        $"Expected {nameof(KernelCommandEnvelope)} or {nameof(KernelEventEnvelope)} but received: \n{message}", KernelCommand.None);
                    isParseError = true;
                }
            }
            catch (Exception ex)
            {
                kernelEvent = new DiagnosticLogEntryProduced(
                    $"Error while parsing Envelope: {message} \n{ex.Message}", KernelCommand.None);
                isParseError = true;
            }

            return kernelCommand is null ? new CommandOrEvent(kernelEvent, isParseError) : new CommandOrEvent(kernelCommand);
        }


        private static bool IsEventEnvelope(JsonElement jsonObject)
        {
            if (jsonObject.TryGetProperty("eventType", out var eventType))
            {
                return !string.IsNullOrWhiteSpace(eventType.GetString());
            }

            return false;
        }

        private static bool IsCommandEnvelope(JsonElement jsonObject)
        {
            if (jsonObject.TryGetProperty("commandType", out var commandType))
            {
                return !string.IsNullOrWhiteSpace(commandType.GetString());
            }

            return false;
        }
    }
}