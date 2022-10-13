// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events;

public class ValueProduced : KernelEvent
{
    [JsonConstructor]
    public ValueProduced(
        string name,
        FormattedValue formattedValue,
        RequestValue command) : base(command)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        Name = name;
        FormattedValue = formattedValue ?? throw new ArgumentNullException(nameof(formattedValue));
    }

    public ValueProduced(
        string name,
        object value,
        RequestValue command) : base(command)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        Value = value;
        Name = name;
    }

    [JsonIgnore] public object Value { get; }

    public string Name { get; }

    public FormattedValue FormattedValue { get; }
}