// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;
using Pocket;

namespace Microsoft.DotNet.Interactive;

internal static class KernelCommandExtensions
{
    internal static void StampRoutingSlip(this KernelCommand command, Uri uri)
    {
        command.RoutingSlip.Stamp(uri);
        command.LogRoutingSlipInfo();
    }

    internal static void StampRoutingSlipAs(this KernelCommand command, Uri uri, string tag)
    {
        command.RoutingSlip.StampAs(uri, tag);
        command.LogRoutingSlipInfo();
    }

    internal static void StampRoutingSlipAsArrived(this KernelCommand command, Uri uri)
    {
        command.RoutingSlip.StampAsArrived(uri);
        command.LogRoutingSlipInfo();
    }

    private static void LogRoutingSlipInfo(this KernelCommand command)
    {
        Logger.Log.Info(
            "Routing slip updated for command {0}: {1}",
            command.ToDisplayString(MimeTypes.Logging),
            command.RoutingSlip.ToDisplayString(MimeTypes.Logging));
    }
}
