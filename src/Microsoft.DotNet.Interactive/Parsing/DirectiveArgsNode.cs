// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

[DebuggerStepThrough]
internal class DirectiveArgsNode : SyntaxNode
{
    internal DirectiveArgsNode(
        SourceText text,
        PolyglotSyntaxTree? syntaxTree) : base(text, syntaxTree)
    {
    }

    public override bool IsSignificant => true;
}