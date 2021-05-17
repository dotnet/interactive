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

                yield return kernelCommand is null? new CommandOrEvent(kernelEvent) : new CommandOrEvent(kernelCommand);
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