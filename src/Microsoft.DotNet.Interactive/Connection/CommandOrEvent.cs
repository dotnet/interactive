// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Connection;

[DebuggerStepThrough]
public class CommandOrEvent
{
    public CommandOrEvent(KernelCommand kernelCommand)
    {
        Command = kernelCommand ?? throw new ArgumentNullException(nameof(kernelCommand));
        Event = null;
    }

    public CommandOrEvent(
        KernelEvent kernelEvent, 
        bool isParseError = false)
    {
        Command = null;
        Event = kernelEvent ?? throw new ArgumentNullException(nameof(kernelEvent));
        IsParseError = isParseError;
    }

    public KernelCommand Command { get; }

    public KernelEvent Event { get; }

    public bool IsParseError { get; }

    public override string ToString()
    {
        if (Command is { })
        {
            return Command.ToString();
        }
        else
        {
            return Event.ToString();
        }
    }
}