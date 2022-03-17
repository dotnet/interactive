// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation
{
    using LinePosition = CodeAnalysis.Text.LinePosition;
    using LinePositionSpan = CodeAnalysis.Text.LinePositionSpan;

    public static class TextSpanExtensions
    {
        public static LinePositionSpan ToLinePositionSpan(this TextSpan span, SourceText text)
        {
            var line = text.Lines.GetLineFromPosition(span.Start);
            var col = span.Start - line.Start;

            var endLine = text.Lines.GetLineFromPosition(span.End);
            var endcol = span.End - endLine.Start;

            return new LinePositionSpan(
               new LinePosition(line.LineNumber, col),
               new LinePosition(endLine.LineNumber, endcol)
            );
        }
    }
}

