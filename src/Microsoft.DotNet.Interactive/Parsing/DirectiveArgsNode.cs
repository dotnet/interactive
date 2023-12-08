// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
<<<<<<<< HEAD:src/Microsoft.DotNet.Interactive/Parsing/DirectiveParameterNameNode.cs
========

using System.Diagnostics;
>>>>>>>> 93c2eb64e (it builds):src/Microsoft.DotNet.Interactive/Parsing/DirectiveArgsNode.cs
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

<<<<<<<< HEAD:src/Microsoft.DotNet.Interactive/Parsing/DirectiveParameterNameNode.cs
internal class DirectiveParameterNameNode : SyntaxNode
{
    internal DirectiveParameterNameNode(SourceText sourceText, SyntaxTree syntaxTree) : base(sourceText, syntaxTree)
========
[DebuggerStepThrough]
internal class DirectiveArgsNode : SyntaxNode
{
    internal DirectiveArgsNode(
        SourceText text,
        PolyglotSyntaxTree? syntaxTree) : base(text, syntaxTree)
>>>>>>>> 93c2eb64e (it builds):src/Microsoft.DotNet.Interactive/Parsing/DirectiveArgsNode.cs
    {
    }

    public override bool IsSignificant => true;
}