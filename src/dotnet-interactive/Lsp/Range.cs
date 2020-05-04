// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.App.Lsp
{
    public class Range
    {
        public Position Start { get; }
        public Position End { get; }

        public Range(Position start, Position end)
        {
            Start = start;
            End = end;
        }

        public static Range FromLinePositionSpan(LinePositionSpan? linePositionSpan)
        {
            if (!linePositionSpan.HasValue)
            {
                return null;
            }

            return new Range(
                Position.FromLinePosition(linePositionSpan.GetValueOrDefault().Start),
                Position.FromLinePosition(linePositionSpan.GetValueOrDefault().End));
        }
    }
}
