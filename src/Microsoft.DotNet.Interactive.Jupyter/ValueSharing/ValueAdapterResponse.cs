// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing;

public interface IValueAdapterResponseBody { }

[ValueAdapterMessageType(ValueAdapterMessageType.Response)]
public abstract class ValueAdapterResponse<T> : ValueAdapterCommandMessage where T : IValueAdapterResponseBody
{
    [JsonPropertyName("success")]
    public bool Success { get; }

    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T Body { get; }

    public ValueAdapterResponse(bool success, T body): base(ValueAdapterMessageType.Response)
    {
        Success = success;
        Body = body;
    }
}
