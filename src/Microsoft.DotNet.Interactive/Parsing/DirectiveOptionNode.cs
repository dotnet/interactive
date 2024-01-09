// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class DirectiveOptionNode : SyntaxNode
{
    internal DirectiveOptionNode(SourceText sourceText, SyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public DirectiveOptionNameNode? OptionNameNode { get; private set; }

    public DirectiveArgumentNode? ArgumentNode { get; private set; }

    public void Add(DirectiveOptionNameNode node)
    {
        AddInternal(node);
        OptionNameNode = node;
    }
    
    public void Add(DirectiveArgumentNode node)
    {
        AddInternal(node);
        ArgumentNode = node;
    }
}