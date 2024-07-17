// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

[DebuggerStepThrough]
internal class LanguageNode : TopLevelSyntaxNode
{
    internal LanguageNode(
        string targetKernelName,
        SourceText sourceText,
        PolyglotSyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
        TargetKernelName = targetKernelName;
    }

    public override bool IsSignificant => true;

    internal void Add(SyntaxNode node, bool addBefore) => AddInternal(node, addBefore);
}