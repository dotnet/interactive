// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing;

public abstract class ValueAdapterCommandMessage: ValueAdapterMessage 
{
    private static readonly IReadOnlyDictionary<Tuple<string, string>, Type> _commandToClrType;
    private static readonly IReadOnlyDictionary<Type, Tuple<string, string>> _clrTypeToCommand;

    private string _command;

    static ValueAdapterCommandMessage()
    {
        var commandImplementations = typeof(ValueAdapterCommandMessage).Assembly.GetExportedTypes().Where(t =>
            t.IsAbstract == false && typeof(ValueAdapterCommandMessage).IsAssignableFrom(t)).ToList();

        var commandToClrType = new Dictionary<Tuple<string, string>, Type>();
        var clrTypeToCommand = new Dictionary<Type, Tuple<string, string>>();

        foreach (var commandImpl in commandImplementations)
        {
            var command = commandImpl.GetCustomAttribute<ValueAdapterCommandAttribute>(true);
            var messageType = commandImpl.GetCustomAttribute<ValueAdapterMessageTypeAttribute>(true);

            if (command is not null && messageType is not null)
            {
                var key = new Tuple<string, string>(messageType.Name, command.Name);
                commandToClrType[key] = commandImpl;
                clrTypeToCommand[commandImpl] = key;
            }
        }

        _commandToClrType = commandToClrType;
        _clrTypeToCommand = clrTypeToCommand;
    }

    [JsonPropertyName("command")]
    public string Command => _command ?? (_command = _clrTypeToCommand[GetType()].Item2);


    public ValueAdapterCommandMessage(string type): base(type)
    {
    }

    public static ValueAdapterCommandMessage FromDataDictionary(IReadOnlyDictionary<string, object> data)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (!data.TryGetValue("type", out object type))
        {
            throw new ArgumentException($"dictionary does not contain the type key.");
        }

        if (!data.TryGetValue("command", out object command))
        {
            throw new ArgumentException($"dictionary does not contain the command key.");
        }

        if (_commandToClrType.TryGetValue(new Tuple<string, string>(type.ToString(), command.ToString()), out var supportedType))
        {
            var jsonString = JsonSerializer.Serialize(data);
            return JsonSerializer.Deserialize(jsonString, supportedType) as ValueAdapterCommandMessage;
        }

        return null;
    }
}
