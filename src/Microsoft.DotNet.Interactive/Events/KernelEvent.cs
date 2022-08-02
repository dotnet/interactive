// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events;

public abstract class KernelEvent
{
    private readonly RoutingSlip _routingSlip;

    protected KernelEvent(KernelCommand command)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        _routingSlip = new RoutingSlip();
    }


    [JsonIgnore]
    public KernelCommand Command { get; }

    [JsonIgnore] public IReadOnlyCollection<Uri> RoutingSlip => _routingSlip;

    public override string ToString()
    {
        return $"{GetType().Name}";
    }

    public bool TryAddToRoutingSlip(Uri uri)
    {
        return _routingSlip.TryAdd(uri);
    }
}