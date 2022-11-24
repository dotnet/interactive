// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events;

public class CommandCancelled : KernelEvent
{
    [JsonIgnore]
    public KernelCommand CancelledCommand { get; }

    [JsonConstructor]
    public CommandCancelled(Cancel cancel) : base(cancel)
    {
           
    }

    public CommandCancelled(Cancel cancel, KernelCommand cancelledCommand) : base(cancel)
    {
        CancelledCommand = cancelledCommand;
    }
}