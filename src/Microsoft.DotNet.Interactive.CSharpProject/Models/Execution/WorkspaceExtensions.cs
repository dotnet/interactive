// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.CSharpProject.Models.Execution;

public static class WorkspaceExtensions
{
    public static ProjectFileContent GetContentFromBufferId(this Workspace workspace, BufferId bufferId)
    {
        if (bufferId is null)
        {
            throw new ArgumentNullException(nameof(bufferId));
        }

        return workspace.Files.FirstOrDefault(f => f.Name == bufferId.FileName);
    }

    public static int GetAbsolutePositionForBufferByIdOrSingleBufferIfThereIsOnlyOne(
        this Workspace workspace,
        BufferId bufferId = null)
    {
        // TODO: (GetAbsolutePositionForGetBufferWithSpecifiedIdOrSingleBufferIfThereIsOnlyOne) this concept should go away

        var buffer = workspace.GetBufferWithSpecifiedIdOrSingleBufferIfThereIsOnlyOne(bufferId);

        return buffer.AbsolutePosition;
    }

    internal static (int line, int column, int absolutePosition) GetTextLocation(
        this Workspace workspace,
        BufferId bufferId)
    {
        var fileContent = workspace.GetContentFromBufferId(bufferId);
        var absolutePosition = GetAbsolutePositionForBufferByIdOrSingleBufferIfThereIsOnlyOne(workspace, bufferId);

        var src = SourceText.From(fileContent.Text);
        var line = src.Lines.GetLineFromPosition(absolutePosition);

        return (line: line.LineNumber, column: absolutePosition - line.Start, absolutePosition);
    }
}