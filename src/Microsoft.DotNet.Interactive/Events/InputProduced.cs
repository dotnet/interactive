// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events;

public class InputProduced : KernelEvent
{
    public InputProduced(string value, RequestInput command)
        : base(command)
    {
        Value = value;
    }

    public string Value { get; }
}