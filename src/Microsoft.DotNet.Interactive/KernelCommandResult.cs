// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive;

[TypeFormatterSource(typeof(MessageDiagnosticsFormatterSource))]
public class KernelCommandResult
{
    private readonly List<KernelEvent> _events;

    public KernelCommandResult(KernelCommand command, IEnumerable<KernelEvent> events = null)
    {
        Command = command;

        _events = new();
        if (events is not null)
        {
            _events.AddRange(events);
        }
    }

    public KernelCommand Command { get; }

    public IReadOnlyList<KernelEvent> Events => _events;

    internal void AddEvent(KernelEvent @event) => _events.Add(@event);
}