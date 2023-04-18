// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Commands;

public class SendValue : KernelCommand
{
    public SendValue(
        string name,
        object value,
        FormattedValue formattedValue = null,
        string targetKernelName = null) : base(targetKernelName)
    {
        if (formattedValue is null)
        {
            formattedValue = FormattedValue.CreateSingleFromObject(value, JsonFormatter.MimeType);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        Name = name;
        Value = value;
        FormattedValue = formattedValue;
    }

    public FormattedValue FormattedValue { get; }

    public string Name { get; }

    [JsonIgnore] 
    public object Value { get; }
}