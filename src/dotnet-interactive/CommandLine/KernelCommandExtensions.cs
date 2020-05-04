// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.App.CommandLine
{
    internal static class KernelCommandExtensions
    {
        internal const string PublishInternalEventsKey = "publish-internal-events";

        internal static void PublishInternalEvents(
            this IKernelCommand command)
        {
            command.Properties[PublishInternalEventsKey] = true;
        }
    }
}