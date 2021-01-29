// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class CommandSucceeded : KernelEvent
    {

        public CommandSucceeded(KernelCommand command) : base(command)
        {
        }

        public override string ToString() => $"{nameof(CommandSucceeded)}: {Command}";
    }
}