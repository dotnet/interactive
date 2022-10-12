// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using System;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection;

internal class SetValue : KernelCommand
{
    public object Value { get; }

    public string Name { get; }
    public FormattedValue FormattedValue { get; }

    public SetValue(object value,
        string name,
        FormattedValue formattedValue)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        Value = value;
        Name = name;
        FormattedValue = formattedValue ?? throw new ArgumentNullException(nameof(formattedValue));
    }
}
