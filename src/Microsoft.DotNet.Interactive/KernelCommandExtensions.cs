// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive;

public static class KernelCommandExtensions
{
    internal const string IdKey = "id";
    internal const string PublishInternalEventsKey = "publish-internal-events";

    // FIX: (KernelCommandExtensions) move these to KernelCommand

    public static void PublishInternalEvents(
        this KernelCommand command)
    {
        command.Properties[PublishInternalEventsKey] = true;
    }

    internal static void SetId(
        this KernelCommand command,
        string id)
    {
        command.Properties[IdKey] = id;
    }

    internal static string GetOrCreateId(this KernelCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (command.Properties.TryGetValue(IdKey, out var value))
        {
            return (string)value;
        }

        var id = Guid.NewGuid().ToString("N");
        command.SetId(id);
        return id;
    }

    internal static bool IsEquivalentTo(this KernelCommand src, KernelCommand other)
    {
        return ReferenceEquals(src, other)
               || src.GetOrCreateId() == other.GetOrCreateId();
    }
}