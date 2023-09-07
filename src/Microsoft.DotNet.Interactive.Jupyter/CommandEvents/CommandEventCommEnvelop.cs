// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.CommandEvents;

internal class CommandEventCommEnvelop
{
    [JsonPropertyName("commandOrEvent")]
    public string CommandOrEventEnvelop { get; }

    [JsonPropertyName("type")]
    public string Type { get; }

    [JsonIgnore]
    public IKernelEventEnvelope EventEnvelope { get; }

    [JsonIgnore]
    public IKernelCommandEnvelope CommandEnvelope { get; }

    [JsonConstructor]
    public CommandEventCommEnvelop(string commandOrEventEnvelop, string type)
    {
        CommandOrEventEnvelop = commandOrEventEnvelop;
        Type = type;
        if (type == "event")
        {
            EventEnvelope = KernelEventEnvelope.Deserialize(commandOrEventEnvelop);
        }
        else if (type == "command")
        {
            CommandEnvelope = KernelCommandEnvelope.Deserialize(commandOrEventEnvelop);
        }
    }

    public CommandEventCommEnvelop(KernelCommand command)
    {
        CommandOrEventEnvelop = KernelCommandEnvelope.Serialize(command);
        Type = "command";
    }

    public CommandEventCommEnvelop(KernelEvent @event)
    {
        CommandOrEventEnvelop = KernelEventEnvelope.Serialize(@event);
        Type = "event";
    }

    public virtual IReadOnlyDictionary<string, object> ToDictionary()
    {
        Dictionary<string, object> dictionary = null;
        try
        {
            var jsonString = JsonSerializer.Serialize(this, GetType(), JsonFormatter.SerializerOptions);
            dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString, JsonFormatter.SerializerOptions);

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return dictionary ?? new Dictionary<string, object>();
    }

    public static CommandEventCommEnvelop FromDataDictionary(IReadOnlyDictionary<string, object> data)
    {
        if (data == null)
        {
            return null;
        }

        var jsonString = JsonSerializer.Serialize(data);
        return JsonSerializer.Deserialize<CommandEventCommEnvelop>(jsonString);
    }
}
