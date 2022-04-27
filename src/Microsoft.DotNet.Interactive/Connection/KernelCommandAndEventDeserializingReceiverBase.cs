// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Connection;

public abstract class KernelCommandAndEventDeserializingReceiverBase : KernelCommandAndEventReceiverBase
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