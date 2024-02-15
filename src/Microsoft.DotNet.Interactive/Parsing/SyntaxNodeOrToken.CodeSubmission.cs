// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

internal abstract partial class SyntaxNodeOrToken
{
    private const string DiagnosticCategory = "DNI";

    private protected SyntaxNodeOrToken(SourceText sourceText, SyntaxTree syntaxTree)
    {
        SourceText = sourceText;
        SyntaxTree = (PolyglotSyntaxTree)syntaxTree;
    }

    public PolyglotSyntaxTree SyntaxTree { get; }

    public string? GetKernelScope()
    {
        if (AncestorsAndSelf().OfType<TopLevelSyntaxNode>().FirstOrDefault() is { } topLevelParent)
        {
            return topLevelParent.TargetKernelName;
        }

        return null;
    }

    public KernelInfo? GetKernelInfo()
    {
        if (GetKernelScope() is { } targetKernelName &&
            SyntaxTree.ParserConfiguration.KernelInfos.TryGetValue(targetKernelName, out var kernelInfo))
        {
            return kernelInfo;
        }

        return null;
    }
}   