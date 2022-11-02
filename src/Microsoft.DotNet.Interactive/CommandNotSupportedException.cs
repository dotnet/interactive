// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive;

public class CommandNotSupportedException : Exception
{
    public CommandNotSupportedException(Type commandType, Kernel kernel) : base($"Kernel {kernel} does not support command type {commandType.Name}.")
    {
    }
}