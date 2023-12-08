// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

internal class HttpHeaderNode : HttpSyntaxNode
{
    internal HttpHeaderNode(
        SourceText sourceText,
        HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public HttpHeaderNameNode? NameNode { get; private set; }

    public HttpHeaderSeparatorNode? SeparatorNode { get; private set; }

    public HttpHeaderValueNode? ValueNode { get; private set; }

    public void Add(HttpHeaderNameNode node)
    {
        if (NameNode is not null)
        {
            throw new InvalidOperationException($"{nameof(NameNode)} was already added.");
        }

        NameNode = node;
        AddInternal(node);
    }

    public void Add(HttpHeaderSeparatorNode node)
    {
        if (SeparatorNode is not null)
        {
            throw new InvalidOperationException($"{nameof(SeparatorNode)} was already added.");
        }

        SeparatorNode = node;
        AddInternal(node);
    }

    public void Add(HttpHeaderValueNode node)
    {
        if (ValueNode is not null)
        {
            throw new InvalidOperationException($"{nameof(ValueNode)} was already added.");
        }

        ValueNode = node;
        AddInternal(node);
    }
}