// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.LanguageService
{
    public static class KernelInvocationContextExtensions
    {
        public static void PublishHoverTextResponse(this KernelInvocationContext context, RequestHoverText command, IReadOnlyCollection<FormattedValue> content, LinePositionSpan? linePositionSpan = null)
        {
            var response = new HoverTextProduced(command, content, linePositionSpan);
            context.Publish(response);
        }

        public static void PublishHoverTextResponse(this KernelInvocationContext context, RequestHoverText command, FormattedValue value, LinePositionSpan? linePositionSpan = null)
        {
            context.PublishHoverTextResponse(command, new[] { value }, linePositionSpan);
        }

        public static void PublishHoverTextMarkdownResponse(this KernelInvocationContext context, RequestHoverText command, string content, LinePositionSpan? linePositionSpan = null)
        {
            context.PublishHoverTextResponse(command, new FormattedValue("text/markdown", content), linePositionSpan);
        }
    }
}
