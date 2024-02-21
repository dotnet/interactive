// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

internal abstract class TopLevelSyntaxNode : SyntaxNode
{
    internal TopLevelSyntaxNode(string targetKernelName, SourceText sourceText, SyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
        TargetKernelName = targetKernelName;
        CommandScope = targetKernelName; // QUESTION: (TopLevelSyntaxNode) are these concepts redundant at this level?
    }

    public string TargetKernelName { get; }

    internal string CommandScope { get; }
}