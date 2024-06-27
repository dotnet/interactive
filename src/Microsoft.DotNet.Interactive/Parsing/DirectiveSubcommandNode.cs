// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class DirectiveSubcommandNode : SyntaxNode
{
    internal DirectiveSubcommandNode(SourceText sourceText, SyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public DirectiveNameNode? NameNode { get; private set; }

    public bool HasParameters { get; private set; }

    public void Add(DirectiveNameNode node)
    {
        NameNode = node;
        AddInternal(node);
    }

    public void Add(DirectiveParameterNode node)
    {
        AddInternal(node);
        HasParameters = true;
    }

    public void Add(DirectiveParameterValueNode valueNode)
    {
        // FIX: (Add) test implicit named parameters on subcommands
        AddInternal(valueNode);
        HasParameters = true;
    }
}