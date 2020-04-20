// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.LanguageService
{
    public static class ContextExtensions
    {
        public static void PublishHoverResponse(this KernelInvocationContext context, RequestHoverText command, MarkupContent contents, Range range)
        {
            var response = new HoverTextProduced(command, contents, range);
            context.Publish(response);
        }

        public static void PublishEmptyLanguageServiceResponse(this KernelInvocationContext context, IKernelCommand command)
        {
            var response = new LanguageServiceNoResultProduced(command);
            context.Publish(response);
        }
    }
}
