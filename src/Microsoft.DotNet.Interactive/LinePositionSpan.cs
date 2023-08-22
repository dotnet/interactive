// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive;

public class LinePositionSpan : IEquatable<LinePositionSpan>
{
    public LinePositionSpan(LinePosition start, LinePosition end)
    {
        Start = start;
        End = end;
    }

    public LinePosition Start { get; }

    public LinePosition End { get; }

    public LinePositionSpan SubtractLineOffset(LinePosition offset)
    {
        return new LinePositionSpan(
            Start.SubtractLineOffset(offset),
            End.SubtractLineOffset(offset));
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as LinePositionSpan);
    }

    public bool Equals(LinePositionSpan other)
    {
        return other is not null && this == other;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Start, End);
    }

    public override string ToString()
    {
        return $"[{Start}-{End})";
    }

    public static bool operator ==(LinePositionSpan a, LinePositionSpan b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Start == b.Start
               && a.End == b.End;
    }

    public static bool operator !=(LinePositionSpan a, LinePositionSpan b)
    {
        return !(a == b);
    }

    public static LinePositionSpan FromCodeAnalysisLinePositionSpan(CodeAnalysis.Text.LinePositionSpan linePositionSpan)
    {
        return new LinePositionSpan(LinePosition.FromCodeAnalysisLinePosition(linePositionSpan.Start), LinePosition.FromCodeAnalysisLinePosition(linePositionSpan.End));
    }
}