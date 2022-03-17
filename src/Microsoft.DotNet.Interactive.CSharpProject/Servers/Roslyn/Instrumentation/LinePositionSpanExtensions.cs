// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation
{
    using LinePositionSpan = CodeAnalysis.Text.LinePositionSpan;

    public static class LinePositionSpanExtensions
    {
        public static bool ContainsLine(this LinePositionSpan viewportSpan, int line) =>
            line < viewportSpan.End.Line && line > viewportSpan.Start.Line;
    }
}
