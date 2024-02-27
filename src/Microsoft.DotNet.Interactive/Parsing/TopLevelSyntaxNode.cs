// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

internal abstract class TopLevelSyntaxNode : SyntaxNode
{
    internal TopLevelSyntaxNode(SourceText sourceText, SyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public string? TargetKernelName { get; set; }

    internal string? CommandScope => TargetKernelName; // QUESTION: (TopLevelSyntaxNode) are these concepts redundant at this level?
}