// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing;

public interface IValueAdapterRequestArguments
{
}

// Based on the Microsoft DAP Request type https://microsoft.github.io/debug-adapter-protocol/specification 
[ValueAdapterMessageType(ValueAdapterMessageType.Request)]
public abstract class ValueAdapterRequest<T>: ValueAdapterCommandMessage where T : IValueAdapterRequestArguments
{

    [JsonPropertyName("arguments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T Arguments { get; }


    public ValueAdapterRequest(T arguments) : base(ValueAdapterMessageType.Request)
    {
        Arguments = arguments;
    }
}
