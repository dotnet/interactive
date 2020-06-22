// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Extensions
{
    internal static class EventExtensions
    {
        public static LinePositionSpan? CalculateLineOffsetFromParentCommand(this KernelEvent @event, LinePositionSpan? initialRange)
        {
            if (!initialRange.HasValue)
            {
                return null;
            }

            var range = initialRange.GetValueOrDefault();
            var requestCommand = @event.Command as LanguageServiceCommand;
            if (requestCommand?.Parent is LanguageServiceCommand parentRequest)
            {
                var requestPosition = requestCommand.Position;
                var lineOffset = parentRequest.Position.Line - requestPosition.Line;
                return new LinePositionSpan(
                    new LinePosition(range.Start.Line + lineOffset, range.Start.Character),
                    new LinePosition(range.End.Line + lineOffset, range.End.Character));
            }

            return range;
        }
    }
}
