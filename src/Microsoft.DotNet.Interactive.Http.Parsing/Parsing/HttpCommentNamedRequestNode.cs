#nullable enable

using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing;
internal class HttpCommentNamedRequestNode : HttpSyntaxNode
{
    public HttpCommentNamedRequestNode(SourceText sourceText, HttpSyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public HttpCommentNamedRequestNameNode? NameNode { get; private set; }

    public HttpCommentNamedRequestValueNode? ValueNode { get; private set; }

    public void Add(HttpCommentNamedRequestNameNode node)
    {
        if (NameNode is not null)
        {
            throw new InvalidOperationException($"{nameof(NameNode)} was already added.");
        }

        NameNode = node;
        AddInternal(node);
    }

    public void Add(HttpCommentNamedRequestValueNode node)
    {
        if (ValueNode is not null)
        {
            throw new InvalidOperationException($"{nameof(ValueNode)} was already added.");
        }

        ValueNode = node;
        AddInternal(node);
    }
}
