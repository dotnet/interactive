#nullable enable

using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing;
internal class HttpNamedRequestNode : HttpSyntaxNode
{
    public HttpNamedRequestNode(SourceText sourceText, HttpSyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public HttpNamedRequestNameNode? ValueNode { get; private set; }

    public void Add(HttpNamedRequestNameNode node)
    {
        if (ValueNode is not null)
        {
            throw new InvalidOperationException($"{nameof(ValueNode)} was already added.");
        }

        ValueNode = node;
        AddInternal(node);
    }
}
