// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive;

public class NoSuitableKernelException : Exception
{
    public NoSuitableKernelException(KernelCommand command) : base($"No kernel found for {command} with target kernel '{command.TargetKernelName}'.")
    {
    }

    public NoSuitableKernelException(string message) : base(message)
    {
    }
}