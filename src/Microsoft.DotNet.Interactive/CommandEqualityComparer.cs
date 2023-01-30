// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive;

[DebuggerStepThrough]
internal class CommandEqualityComparer : IEqualityComparer<KernelCommand>
{
    public static CommandEqualityComparer Instance { get; } = new();

    public bool Equals(KernelCommand xCommand, KernelCommand yCommand)
    {
        if (ReferenceEquals(xCommand, yCommand))
        {
            return true;
        }

        if (xCommand.Properties.TryGetValue(KernelCommandExtensions.IdKey, out var xCommandId) &&
            xCommandId is string xCommandIdString &&
            yCommand.Properties.TryGetValue(KernelCommandExtensions.IdKey, out var yCommandId) &&
            yCommandId is string yCommandIdString)
        {
            return string.Equals(xCommandIdString, yCommandIdString, StringComparison.Ordinal);
        }

        return false;
    }

    public int GetHashCode(KernelCommand obj)
    {
        return obj.GetOrCreateId().GetHashCode();
    }
}