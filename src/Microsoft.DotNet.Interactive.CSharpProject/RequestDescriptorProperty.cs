// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class RequestDescriptorProperty
{
    [JsonProperty("name")]
    public string Name { get; }
    [JsonProperty("value")]
    public object Value { get; }

    public RequestDescriptorProperty(string name, object value = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        Name = name;
        Value = value;
    }
}