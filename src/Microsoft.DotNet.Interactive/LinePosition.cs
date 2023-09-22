// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive;

public record LinePosition(int Line, int Character)
{
    public LinePosition SubtractLineOffset(LinePosition offset)
    {
        return new LinePosition(Line - offset.Line, Character);
    }

    public override string ToString()
    {
        return $"({Line}, {Character})";
    }

    public CodeAnalysis.Text.LinePosition ToCodeAnalysisLinePosition()
    {
        return new CodeAnalysis.Text.LinePosition(Line, Character);
    }

    public static LinePosition FromCodeAnalysisLinePosition(CodeAnalysis.Text.LinePosition linePosition)
    {
        return new LinePosition(linePosition.Line, linePosition.Character);
    }
}