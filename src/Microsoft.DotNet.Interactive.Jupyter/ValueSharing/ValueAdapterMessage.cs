// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing;

public abstract class ValueAdapterMessage
{
    [JsonPropertyName("type")]
    public string Type { get; }

    public ValueAdapterMessage(string messageType)
    {
        Type = messageType;
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
}
