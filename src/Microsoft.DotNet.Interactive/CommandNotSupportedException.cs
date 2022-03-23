// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive;

public class CommandNotSupportedException : Exception
{
    public CommandNotSupportedException(KernelCommand command, Kernel kernel) : base($"Kernel {kernel} does not support command type {command.GetType().Name}.")
    {
    }
}