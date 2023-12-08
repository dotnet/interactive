// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

[DebuggerStepThrough]
internal class LanguageNode : SyntaxNode
{
    internal LanguageNode(
        SourceText sourceText,
        PolyglotSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public override bool IsSignificant => true;

    internal string CommandScope { get; set; }

    public override bool IsSignificant => true;
}