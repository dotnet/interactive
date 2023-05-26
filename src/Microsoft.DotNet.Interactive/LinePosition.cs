// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive;

public class LinePosition : IEquatable<LinePosition>
{
    public int Line { get; }
    public int Character { get; }

    public LinePosition(int line, int character)
    {
        Line = line;
        Character = character;
    }

    public CodeAnalysis.Text.LinePosition ToCodeAnalysisLinePosition()
    {
        return new CodeAnalysis.Text.LinePosition(Line, Character);
    }

    public LinePosition SubtractLineOffset(LinePosition offset)
    {
        return new LinePosition(Line - offset.Line, Character);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as LinePosition);
    }

    public bool Equals(LinePosition other)
    {
        return other is { } && this == other;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Line, Character);
    }

    public override string ToString()
    {
        return $"({Line}, {Character})";
    }

    public static bool operator ==(LinePosition a, LinePosition b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Line == b.Line
               && a.Character == b.Character;
    }

    public static bool operator !=(LinePosition a, LinePosition b)
    {
        return !(a == b);
    }

    public static LinePosition FromCodeAnalysisLinePosition(CodeAnalysis.Text.LinePosition linePosition)
    {
        return new LinePosition(linePosition.Line, linePosition.Character);
    }
}