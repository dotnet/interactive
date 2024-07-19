// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands;

public abstract class LanguageServiceCommand : KernelCommand
{
    private protected LanguageServiceCommand(
        string code,
        LinePosition linePosition,
        string targetKernelName = null)
        : base(targetKernelName)
    {
        Code = code;
        LinePosition = linePosition;
    }
        
    private protected LanguageServiceCommand(
        TopLevelSyntaxNode syntaxNode,
        LinePosition linePosition,
        int originalPosition)
        : base(syntaxNode.TargetKernelName)
    {
        Code = syntaxNode.Text;
        SyntaxNode = syntaxNode;
        LinePosition = linePosition;
        OriginalPosition = originalPosition;
        SchedulingScope = SchedulingScope.Parse(syntaxNode.CommandScope);

    }

    public string Code { get; protected set; }

    public LinePosition LinePosition { get; protected set; }

    internal int OriginalPosition { get; }

    internal abstract LanguageServiceCommand AdjustForCommandSplit(
        TopLevelSyntaxNode syntaxNode,
        LinePosition adjustedPosition,
        int originalPosition);

    internal TopLevelSyntaxNode SyntaxNode { get; }
}