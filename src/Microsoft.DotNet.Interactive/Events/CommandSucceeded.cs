// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events;

public sealed class CommandSucceeded : KernelCommandCompletionEvent
{
    public CommandSucceeded(
        KernelCommand command,
        int executionOrder = 0) : base(command, executionOrder)
    {
    }

    public override string ToString() => $"{nameof(CommandSucceeded)}: {Command}";
}