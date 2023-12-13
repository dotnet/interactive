// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

[DebuggerStepThrough]
internal class PolyglotSubmissionNode : SyntaxNode
{
    internal PolyglotSubmissionNode(
        SourceText sourceText,
        PolyglotSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public void Add(TopLevelSyntaxNode node)
    {
        AddInternal(node);
    }
}