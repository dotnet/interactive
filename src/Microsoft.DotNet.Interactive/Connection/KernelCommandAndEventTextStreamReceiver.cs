// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class CommandOrEvent
    {
        public KernelCommand Command { get; }
        public KernelEvent Event { get; }

        public CommandOrEvent(KernelCommand kernelCommand, KernelEvent kernelEvent)
        {
            if (!(kernelCommand is not null ^ kernelEvent is not null))
            {
                throw new InvalidOperationException(
                    $"Only one of {nameof(kernelCommand)} and {nameof(kernelEvent)} must be not null");
            }
            Command = kernelCommand;
            Event = kernelEvent;
        }
    };

    public interface IKernelCommandAndEventReceiver
    {
        IAsyncEnumerable<CommandOrEvent> CommandsOrEventsAsync();
    }

    public class KernelCommandAndEventTextStreamReceiver : IKernelCommandAndEventReceiver
    {
        private readonly TextReader _reader;

        public KernelCommandAndEventTextStreamReceiver(TextReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public async IAsyncEnumerable<CommandOrEvent> CommandsOrEventsAsync()
        {
            while (true)
            {
                KernelCommand kernelCommand = null;
                KernelEvent kernelEvent = null;
                
                var message = await _reader.ReadLineAsync();
               
                try
                {
                    var jsonObject = JsonDocument.Parse(message).RootElement;
                    if (IsEventEnvelope(jsonObject))
                    {
                        var kernelEventEnvelope = KernelEventEnvelope.Deserialize(jsonObject);
                        kernelEvent = kernelEventEnvelope.Event;
                    }
                    else if(IsCommandEnvelope(jsonObject))
                    {
                        var kernelCommandEnvelope = KernelCommandEnvelope.Deserialize(jsonObject);
                        kernelCommand = kernelCommandEnvelope.Command;
                    }
                    else
                    {
                        kernelEvent = new DiagnosticLogEntryProduced(
                            $"Expected {nameof(KernelCommandEnvelope)} or {nameof(KernelEventEnvelope)} but received: \n{message}", KernelCommand.None);
                    }
                }
                catch (Exception ex)
                {
                    kernelEvent = new DiagnosticLogEntryProduced(
                        $"Error while parsing Envelope: {ex.Message}\n{message}", KernelCommand.None);
                }

                yield return new CommandOrEvent(kernelCommand, kernelEvent);
            }
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