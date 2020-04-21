// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.LanguageService
{
    public static class KernelInvocationContextExtensions
    {
        public static void PublishHoverResponse(this KernelInvocationContext context, RequestHoverText command, MarkupContent contents, LinePositionSpan linePositionSpan)
        {
            var response = new HoverTextProduced(command, contents, linePositionSpan);
            context.Publish(response);
        }
    }
}
