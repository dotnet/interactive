// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Events;

[TypeFormatterSource(typeof(KernelEventLoggingFormatterSource))]
public abstract class KernelEvent
{
    protected KernelEvent(KernelCommand command)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
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
