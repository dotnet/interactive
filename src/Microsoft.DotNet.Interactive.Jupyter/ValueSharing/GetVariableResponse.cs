﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing;

public class GetVariableResponseBody : IValueAdapterResponseBody
{
    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("type")]
    public string Type { get; }

    [JsonPropertyName("value")]
    public object Value { get; }

    public GetVariableResponseBody(string name, string type, object value)
    {
        Name = name;
        Type = type;
        Value = value;
    }
}

[ValueAdapterMessageType(ValueAdapterMessageType.Response)]
[ValueAdapterCommand(ValueAdapterCommandTypes.GetVariable)]
public class GetVariableResponse : ValueAdapterResponse<GetVariableResponseBody>
{
    public GetVariableResponse(bool success, GetVariableResponseBody body, string message = null) : base(success, body, message)
    {
    }
}