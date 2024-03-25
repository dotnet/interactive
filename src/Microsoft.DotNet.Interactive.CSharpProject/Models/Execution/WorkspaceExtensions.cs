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
        if (bufferId == null)
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

    public static Workspace AddBuffer(
        this Workspace workspace,
        string id,
        string text) =>
        new Workspace(
            workspace.Usings,
            workspace.Files,
            workspace.Buffers.Concat(new[] { new Buffer(BufferId.Parse(id), text) }).ToArray(),
            workspace.WorkspaceType,
            workspace.Language,
            workspace.IncludeInstrumentation);

    public static Workspace RemoveBuffer(
        this Workspace workspace,
        string id) =>
        new Workspace(
            workspace.Usings,
            workspace.Files,
            workspace.Buffers.Where(b => b.Id.ToString() != id).ToArray(),
            workspace.WorkspaceType,
            workspace.Language,
            workspace.IncludeInstrumentation);

    public static Workspace ReplaceBuffer(
        this Workspace workspace,
        string id,
        string text) =>
        workspace.RemoveBuffer(id).AddBuffer(id, text);

    public static Workspace AddFile(
        this Workspace workspace,
        string name,
        string text) =>
        new Workspace(
            workspace.Usings,
            workspace.Files
                .Concat(new[] { new ProjectFileContent(name, text) })
                .ToArray(),
            workspace.Buffers,
            workspace.WorkspaceType,
            workspace.Language,
            workspace.IncludeInstrumentation);

    public static Workspace ReplaceFile(
        this Workspace workspace,
        string name,
        string text) =>
        new Workspace(
            workspace.Usings,
            workspace.Files
                .Where(f => f.Name != name)
                .Concat(new[] { new ProjectFileContent(name, text) })
                .ToArray(),
            workspace.Buffers,
            workspace.WorkspaceType,
            workspace.Language,
            workspace.IncludeInstrumentation);

}