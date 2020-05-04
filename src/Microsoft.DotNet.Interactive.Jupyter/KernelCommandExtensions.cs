// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    internal static class KernelCommandExtensions
    {
        internal const string PublishInternalEventsKey = "publish-internal-events";

        internal static bool ShouldPublishInternalEvents(
            this IKernelCommand command)
        {
            var returnValue = false;
            if (command.Properties.TryGetValue(PublishInternalEventsKey, out var produceEvents))
            {
                returnValue = (bool) produceEvents;
            }
            else
            {
                if (command is KernelCommandBase commandBase &&
                    commandBase.Parent != null)
                {
                    returnValue = commandBase.Parent.ShouldPublishInternalEvents();
                }
            }

            return returnValue;
        }
    }
}