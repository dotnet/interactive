﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Commands;

public class SendValue : KernelCommand
{
    public SendValue(
        string name,
        object value,
        FormattedValue formattedValue = null,
        string targetKernelName = null) : base(targetKernelName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        Name = name;
        Value = value;
        FormattedValue = formattedValue;
    }

    public FormattedValue FormattedValue { get; internal set; }

    public string Name { get; }

    [JsonIgnore] 
    public object Value { get; }
}