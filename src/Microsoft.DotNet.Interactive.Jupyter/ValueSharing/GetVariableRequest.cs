// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing;

public class GetVariableArguments : IValueAdapterRequestArguments
{

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("type")]
    public string Type { get; }

    public GetVariableArguments(string name, string type)
    {
        Name = name;
        Type = type;
    }
}

[ValueAdapterMessageType(ValueAdapterMessageType.Request)]
[ValueAdapterCommand(ValueAdapterCommandTypes.GetVariable)]
public class GetVariableRequest : ValueAdapterRequest<GetVariableArguments>
{
    public GetVariableRequest(GetVariableArguments arguments): base(arguments)
    {
    }
}
