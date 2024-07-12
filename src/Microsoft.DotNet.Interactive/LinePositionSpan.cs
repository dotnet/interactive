// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive;

public record LinePositionSpan(LinePosition Start, LinePosition End)
{
    public LinePositionSpan SubtractLineOffset(LinePosition offset)
    {
        return new LinePositionSpan(
            Start.SubtractLineOffset(offset),
            End.SubtractLineOffset(offset));
    }

    public override string ToString()
    {
        return $"[{Start}-{End})";
    }

    public CodeAnalysis.Text.LinePositionSpan ToCodeAnalysisLinePositionSpan()
    {
        return new CodeAnalysis.Text.LinePositionSpan(Start.ToCodeAnalysisLinePosition(), End.ToCodeAnalysisLinePosition());
    }

    public static LinePositionSpan FromCodeAnalysisLinePositionSpan(CodeAnalysis.Text.LinePositionSpan linePositionSpan)
    {
        return new LinePositionSpan(LinePosition.FromCodeAnalysisLinePosition(linePositionSpan.Start), LinePosition.FromCodeAnalysisLinePosition(linePositionSpan.End));
    }
}