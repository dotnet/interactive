// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Commands;
using Pocket;

namespace Microsoft.DotNet.Interactive;

internal static class KernelCommandExtensions
{
    internal static void StampRoutingSlipAndLog(this KernelCommand command, Uri uri)
    {
        command.RoutingSlip.Stamp(uri);
        Logger.Log.RoutingSlipInfo(command, uri);
    }

    internal static void StampRoutingSlipAsAndLog(this KernelCommand command, Uri uri, string tag)
    {
        command.RoutingSlip.StampAs(uri, tag);
        Logger.Log.RoutingSlipInfo(command, uri, tag);
    }

    internal static void StampRoutingSlipAsArrivedAndLog(this KernelCommand command, Uri uri)
    {
        command.RoutingSlip.StampAsArrived(uri);
        Logger.Log.RoutingSlipInfo(command, uri, tag: "arrived");
    }

    private static void RoutingSlipInfo(this Logger logger, KernelCommand command, Uri uri, string tag = null)
    {
        if (string.IsNullOrEmpty(tag))
        {
            logger.Info("➡️ {0} {1}", command.GetType().Name, uri.ToString());
        }
        else
        {
            logger.Info("➡️ {0} {1} ({2})", command.GetType().Name, uri.ToString(), tag);
        }
    }
}
