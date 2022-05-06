// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Microsoft.DotNet.Interactive.Documents.ParserServer;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Connection;

internal static class Serializer
{
    static Serializer()
    {
        JsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        JsonSerializerOptions.Converters.Add(new ByteArrayConverter());
        JsonSerializerOptions.Converters.Add(new DataDictionaryConverter());
        JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        JsonSerializerOptions.Converters.Add(new NotebookCellOutputConverter());
        JsonSerializerOptions.Converters.Add(new FileSystemInfoJsonConverter());
    }

    public static JsonSerializerOptions JsonSerializerOptions { get; }

    public static CommandOrEvent DeserializeCommandOrEvent(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var jsonObject = JsonDocument.Parse(json).RootElement;

            if (IsEventEnvelope(jsonObject))
            {
                var kernelEventEnvelope = KernelEventEnvelope.Deserialize(jsonObject);
                return new CommandOrEvent(kernelEventEnvelope.Event);
            }

            if (IsCommandEnvelope(jsonObject))
            {
                var kernelCommandEnvelope = KernelCommandEnvelope.Deserialize(jsonObject);

                return new CommandOrEvent(kernelCommandEnvelope.Command);
            }

            var kernelEvent = new DiagnosticLogEntryProduced(
                $"Expected {nameof(KernelCommandEnvelope)} or {nameof(KernelEventEnvelope)} but received: \n{json}", KernelCommand.None);

            return new CommandOrEvent(kernelEvent, true);
        }
        catch (Exception ex)
        {
            var kernelEvent = new DiagnosticLogEntryProduced(
                $"Error while parsing Envelope: {json} \n{ex.Message}", KernelCommand.None);
            return new CommandOrEvent(kernelEvent, true);
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