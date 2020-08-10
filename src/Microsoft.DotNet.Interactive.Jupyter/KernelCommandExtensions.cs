// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    internal static class KernelCommandExtensions
    {
        private const string PublishInternalEventsKey = "publish-internal-events";

        internal static bool ShouldPublishInternalEvents(
            this KernelCommand command)
        {
            var returnValue = false;
            if (command.Properties.TryGetValue(PublishInternalEventsKey, out var produceEvents))
            {
                returnValue = (bool) produceEvents;
            }
            else if (command?.Parent != null)
            {
                returnValue = command.Parent.ShouldPublishInternalEvents();
            }

            return returnValue;
        }
    }
}