// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.LanguageService
{
    public static class KernelInvocationContextExtensions
    {
        public static void PublishHoverMarkdownResponse(this KernelInvocationContext context, RequestHoverText command, string content, LinePositionSpan linePositionSpan)
        {
            var response = new HoverMarkdownProduced(command, content, linePositionSpan);
            context.Publish(response);
        }

        public static void PublishHoverPlainTextResponse(this KernelInvocationContext context, RequestHoverText command, string content, LinePositionSpan linePositionSpan)
        {
            var response = new HoverPlainTextProduced(command, content, linePositionSpan);
            context.Publish(response);
        }
    }
}
