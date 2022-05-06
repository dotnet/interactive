﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Connection;

public class KernelCommandEnvelope<T> : KernelCommandEnvelope where T : KernelCommand
{
    public KernelCommandEnvelope(T command) : base(command)
    {
        Command = command;
    }

    public T Command { get; }

    public override string CommandType => typeof(T).Name;
}