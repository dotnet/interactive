// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events;

[DebuggerStepThrough]
public abstract class KernelEvent
{
    protected KernelEvent(KernelCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        Command = command.SelfOrFirstUnhiddenAncestor ?? throw new ArgumentException("Command and all ancestors are hidden.");

        RoutingSlip = new EventRoutingSlip();
    }

    [JsonIgnore] 
    public KernelCommand Command { get; }

    [JsonIgnore] 
    public EventRoutingSlip RoutingSlip { get; }

    public override string ToString()
    {
        return $"{GetType().Name}";
    }
}
