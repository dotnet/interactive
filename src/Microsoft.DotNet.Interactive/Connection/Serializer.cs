// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Connection;

public static class Serializer
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
        JsonSerializerOptions.Converters.Add(new DataDictionaryConverter());
        JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        JsonSerializerOptions.Converters.Add(new FileSystemInfoJsonConverter());
        JsonSerializerOptions.Converters.Add(new KernelDirectiveConverter());
    }

    public static JsonSerializerOptions JsonSerializerOptions { get; }

    public static CommandOrEvent DeserializeCommandOrEvent(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var jsonObject = JsonDocument.Parse(json).RootElement;

        return DeserializeCommandOrEvent(jsonObject);
    }

    public static CommandOrEvent DeserializeCommandOrEvent(JsonElement jsonObject)
    {
        if (IsEventEnvelope(jsonObject))
        {
            var kernelEventEnvelope = KernelEventEnvelope.Deserialize(jsonObject);
            return new CommandOrEvent(kernelEventEnvelope.Event);
        }
        else
        {
            var kernelCommandEnvelope = KernelCommandEnvelope.Deserialize(jsonObject);
            return new CommandOrEvent(kernelCommandEnvelope.Command);
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