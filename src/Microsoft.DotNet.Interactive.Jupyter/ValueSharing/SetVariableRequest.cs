// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing;

public class SetVariableArguments : IValueAdapterRequestArguments
{
    // if seq is 0, declare and assign the variable. > 0 means that it's the next chunk and should be 
    // appended to the existing variable. 
    [JsonPropertyName("seq")]
    public int Seq { get; }

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("value")]
    public object Value { get; }

    [JsonPropertyName("type")]
    public string Type { get; }

    public SetVariableArguments(int seq, string name, object value, string type)
    {
        Seq = seq;
        Name = name;
        Value = value;
        Type = type;
    }
}

[ValueAdapterMessageType(ValueAdapterMessageType.Request)]
[ValueAdapterCommand(ValueAdapterCommandTypes.SetVariable)]
public class SetVariableRequest : ValueAdapterRequest<SetVariableArguments>
{
    public SetVariableRequest(SetVariableArguments arguments): base(arguments)
    {
    }
}
